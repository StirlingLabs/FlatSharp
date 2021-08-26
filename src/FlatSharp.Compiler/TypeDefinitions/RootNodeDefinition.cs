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
    /// <summary>
    /// Defines the node at the root of the schema.
    /// </summary>
    internal class RootNodeDefinition : BaseSchemaMember
    {
        public RootNodeDefinition(string fileName) : base("", null)
        {
            this.DeclaringFile = fileName;
        }

        public string? InputHash { get; set; }

        protected override bool SupportsChildren => true;

        protected override void OnWriteCode(CodeWriter writer, CompileContext context)
        {
            writer.AppendLine($@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the FlatSharp FBS to C# compiler (source hash: {this.InputHash})
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
");

            writer.AppendLine("using System;");
            writer.AppendLine("using System.Collections.Generic;");
            writer.AppendLine("using System.Linq;");
            writer.AppendLine("using System.Runtime.CompilerServices;");
            writer.AppendLine("using System.Threading;");
            writer.AppendLine("using System.Threading.Tasks;");
            writer.AppendLine("using FlatSharp;");
            writer.AppendLine("using FlatSharp.Attributes;");
            if (context.NeedsUnsafe)
            {
                writer.AppendLine("using FlatSharp.Unsafe;");
            }

            // disable obsolete warnings. Flatsharp allows marking default constructors
            // as obsolete and we don't want to raise warnings for our own code.
            writer.AppendLine("#pragma warning disable 0618");

            // Test hook to check deeply for nullability violations.
#if NET5_0_OR_GREATER
            if (RoslynSerializerGenerator.EnableStrictValidation && 
                context.Options.NullableWarnings == null)
            {
                context = context with 
                { 
                    Options = context.Options with 
                    { 
                        NullableWarnings = true
                    } 
                };
            }
#endif

            if (context.Options.NullableWarnings == true)
            {
                writer.AppendLine("#nullable enable");
            }
            else
            {
                writer.AppendLine("#nullable enable annotations");
            }

            if (context.CompilePass > CodeWritingPass.PropertyModeling && context.PreviousAssembly is not null)
            {
                context.FullyQualifiedCloneMethodName = CloneMethodsGenerator.GenerateCloneMethodsForAssembly(
                    writer,
                    context.Options,
                    context.PreviousAssembly,
                    context.TypeModelContainer);
            }

            foreach (var child in this.Children.Values)
            {
                child.WriteCode(writer, context);
            }

            writer.AppendLine("#nullable restore");
            writer.AppendLine("#pragma warning restore 0618");
        }
    }
}
