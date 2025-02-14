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
    using Antlr4.Runtime.Misc;
    using System;

    /// <summary>
    /// Parses an enum definition.
    /// </summary>
    internal class EnumVisitor : FlatBuffersBaseVisitor<EnumDefinition?>
    {
        private readonly BaseSchemaMember parent;
        private EnumDefinition? enumDef;

        public EnumVisitor(BaseSchemaMember parent)
        {
            this.parent = parent;
        }

        public override EnumDefinition VisitEnum_decl([NotNull] FlatBuffersParser.Enum_declContext context)
        {
            string typeName = context.IDENT().GetText();

            this.enumDef = new EnumDefinition(
                typeName: typeName,
                underlyingTypeName: context.integer_type_name().GetText(),
                parent: this.parent);

            ErrorContext.Current.WithScope(this.enumDef.Name, () =>
            {
                var metadataVisitor = new MetadataVisitor();
                var metadata = metadataVisitor.VisitMetadata(context.metadata());

                this.enumDef.IsFlags = metadata?.ParseBooleanMetadata(MetadataKeys.BitFlags) ?? false;

                base.VisitEnum_decl(context);
            });

            return this.enumDef;
        }

        public override EnumDefinition? VisitEnumval_decl([NotNull] FlatBuffersParser.Enumval_declContext context)
        {
            EnumDefinition enumDef = this.enumDef ?? throw new InvalidOperationException("enum def not initialized correctly");

            string name = context.IDENT().GetText();
            string? value = context.integer_const()?.GetText();
            enumDef.NameValuePairs.Add((name, value));

            return null;
        }
    }
}
