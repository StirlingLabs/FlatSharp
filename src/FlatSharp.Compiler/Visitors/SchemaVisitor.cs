﻿/*
 * Copyright 2020 James Courtney
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace FlatSharp.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;

    internal class SchemaVisitor : FlatBuffersBaseVisitor<BaseSchemaMember?>
    {
        private (string type, BaseSchemaMember scope)? rootType;
        private (string id, BaseSchemaMember scope)? rootFileIdentifierName;

        private BaseSchemaMember schemaRoot;
        private readonly Stack<BaseSchemaMember> parseStack = new Stack<BaseSchemaMember>();

        public SchemaVisitor(RootNodeDefinition rootNode)
        {
            this.schemaRoot = rootNode;
        }

        public string? CurrentFileName { get; set; }

        public override BaseSchemaMember Visit([NotNull] IParseTree tree)
        {
            this.parseStack.Push(this.schemaRoot);

            base.Visit(tree);
            this.ApplyRootTypeAndFileId();

            return this.schemaRoot;
        }

        public override BaseSchemaMember? VisitNamespace_decl([NotNull] FlatBuffersParser.Namespace_declContext context)
        {
            // Namespaces reset the whole stack.
            while (this.parseStack.Peek() != this.schemaRoot)
            {
                this.parseStack.Pop();
            }

            string[] nsParts = context.IDENT().Select(x => x.GetText()).ToArray();
            this.parseStack.Push(this.GetOrCreateNamespace(nsParts, this.schemaRoot));

            return null;
        }

        public override BaseSchemaMember? VisitType_decl([NotNull] FlatBuffersParser.Type_declContext context)
        {
            var top = this.parseStack.Peek();
            ErrorContext.Current.WithScope(top.FullName, () =>
            {
                FlatSharpInternal.Assert(this.CurrentFileName is not null, "Current file name should not be null");
                new TypeVisitor(top, this.CurrentFileName).Visit(context);
            });

            return null;
        }

        public override BaseSchemaMember? VisitEnum_decl([NotNull] FlatBuffersParser.Enum_declContext context)
        {
            var top = this.parseStack.Peek();
            ErrorContext.Current.WithScope(top.FullName, () =>
            {
                EnumDefinition? def = new EnumVisitor(top).Visit(context);
                FlatSharpInternal.Assert(def is not null, "Enum definition visitor returned null");

                def.DeclaringFile = this.CurrentFileName;
                top.AddChild(def);
            });

            return null;
        }

        public override BaseSchemaMember? VisitUnion_decl([NotNull] FlatBuffersParser.Union_declContext context)
        {
            var top = this.parseStack.Peek();
            ErrorContext.Current.WithScope(top.FullName, () =>
            {
                UnionDefinition? def = new UnionVisitor(top).Visit(context);
                FlatSharpInternal.Assert(def is not null, "Union definition visitor returned null");

                def.DeclaringFile = this.CurrentFileName;
                top.AddChild(def);
            });

            return null;
        }

        public override BaseSchemaMember? VisitRpc_decl([NotNull] FlatBuffersParser.Rpc_declContext context)
        {
            var top = this.parseStack.Peek();
            ErrorContext.Current.WithScope(top.FullName, () =>
            {
                RpcDefinition? def = new RpcVisitor(top).Visit(context);
                FlatSharpInternal.Assert(def is not null, "RPC definition visitor returned null");

                def.DeclaringFile = this.CurrentFileName;
                top.AddChild(def);
            });

            return null;
        }

        public override BaseSchemaMember? VisitRoot_decl([NotNull] FlatBuffersParser.Root_declContext context)
        {
            string typeName = context.IDENT().GetText();
            if (this.rootType is null)
            {
                this.rootType = (typeName, this.parseStack.Peek());
            }
            else
            {
                ErrorContext.Current.RegisterError($"Duplicate root types: '{this.rootType.Value.type}' and '{typeName}'.");
            }

            return null;
        }

        public override BaseSchemaMember? VisitFile_identifier_decl([NotNull] FlatBuffersParser.File_identifier_declContext context)
        {
            string? ident = context.STRING_CONSTANT().ToString()!.Trim('"');
            if (this.rootFileIdentifierName is null)
            {
                this.rootFileIdentifierName = (ident, this.parseStack.Peek());
            }
            else
            {
                ErrorContext.Current.RegisterError($"Duplicate file identifiers: '{this.rootFileIdentifierName.Value.id}' and '{ident}'.");
            }

            return null;
        }

        private BaseSchemaMember GetOrCreateNamespace(Span<string> parts, BaseSchemaMember parent)
        {
            if (!parent.Children.TryGetValue(parts[0], out var existingNode))
            {
                existingNode = new NamespaceDefinition(parts[0], parent);
                existingNode.DeclaringFile = this.CurrentFileName;
                parent.AddChild(existingNode);
            }

            if (parts.Length == 1)
            {
                return existingNode;
            }

            return this.GetOrCreateNamespace(parts.Slice(1), existingNode);
        }

        private void ApplyRootTypeAndFileId()
        {
            TableOrStructDefinition? tableDef = this.ValidateAndLookupRootType();

            if (this.rootType is not null && this.rootFileIdentifierName is not null)
            {
                var (rootTypeName, rootScope) = this.rootType.Value;
                var (fileId, fileIdScope) = this.rootFileIdentifierName.Value;

                if (rootScope != fileIdScope)
                {
                    ErrorContext.Current.RegisterError($"root_type '{rootTypeName}' and file_identifier '{fileId}' were declared under different namespaces.");
                }
                else if (tableDef is not null)
                {
                    if (!string.IsNullOrEmpty(tableDef.FileIdentifier) && tableDef.FileIdentifier != fileId)
                    {
                        ErrorContext.Current.RegisterError($"root_type '{rootTypeName}' has conflicting file identifiers: '{tableDef.FileIdentifier}' and '{fileId}'.");
                    }
                    else
                    {
                        tableDef.FileIdentifier = fileId;
                    }
                }
            }
        }

        private TableOrStructDefinition? ValidateAndLookupRootType()
        {
            if (this.rootType is not null)
            {
                var (rootTypeName, rootScope) = this.rootType.Value;

                if (!rootScope.TryResolveName(rootTypeName, out var node))
                {
                    ErrorContext.Current.RegisterError($"Unable to resolve root_type '{rootTypeName}'.");
                }
                else if (node is TableOrStructDefinition tableOrStruct && tableOrStruct.IsTable)
                {
                    return tableOrStruct;
                }
                else
                {
                    ErrorContext.Current.RegisterError($"root_type '{rootTypeName}' does not reference a table.");
                }
            }

            return null;
        }
    }
}
