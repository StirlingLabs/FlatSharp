﻿/*
 * Copyright 2018 James Courtney
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

namespace FlatSharp
{
    using System;

    /// <summary>
    /// Defines various confiration settings for serializing and deserializing buffers.
    /// </summary>
    public class FlatBufferSerializerOptions
    {
        /// <summary>
        /// Initializes a new instance of FlatBufferSerializerOptions with the given set of flags.
        /// </summary>
        /// <param name="option">The deserialization mode.</param>
        public FlatBufferSerializerOptions(
            FlatBufferDeserializationOption option = FlatBufferDeserializationOption.Default,
            bool devirtualize = true)
        {
            if (!Enum.IsDefined(typeof(FlatBufferDeserializationOption), option))
            {
                throw new ArgumentException(nameof(option), $"The value '{option}' is not defined in '{nameof(FlatBufferDeserializationOption)}'.");
            }

            this.Devirtualize = devirtualize;
            this.DeserializationOption = option;
        }

        /// <summary>
        /// Indicates that FlatSharp should try to devirtualize <see cref="System.Collections.Generic.IList{T}" /> vectors.
        /// Enabling this expands the size of the generated code but allows for must faster execution when using well-known
        /// list types.
        /// </summary>
        public bool Devirtualize { get; }

        /// <summary>
        /// The deserialization mode.
        /// </summary>
        public FlatBufferDeserializationOption DeserializationOption { get; }

        /// <summary>
        /// Indicates if list vectors should have their data cached after reading. This option will cause more allocations
        /// on deserializing, but will improve performance in cases of duplicate accesses to the same indices.
        /// </summary>
        public bool PreallocateVectors =>
            this.DeserializationOption == FlatBufferDeserializationOption.VectorCacheMutable ||
            this.DeserializationOption == FlatBufferDeserializationOption.VectorCache ||
            this.GreedyDeserialize;

        /// <summary>
        /// Indicates if the serializer should generate mutable objects. Mutable objects are "copy-on-write"
        /// and do not modify the underlying buffer.
        /// </summary>
        public bool GenerateMutableObjects =>
            this.DeserializationOption == FlatBufferDeserializationOption.VectorCacheMutable ||
            this.DeserializationOption == FlatBufferDeserializationOption.GreedyMutable;

        /// <summary>
        /// Indicates if deserialization should be greedy.
        /// </summary>
        public bool GreedyDeserialize =>
            this.DeserializationOption == FlatBufferDeserializationOption.Greedy ||
            this.DeserializationOption == FlatBufferDeserializationOption.GreedyMutable;

        /// <summary>
        /// Indicates if properties should be progressively cached as they are read. Lazy and greedy don't need this logic.
        /// </summary>
        public bool PropertyCache => !this.Lazy && !this.GreedyDeserialize;

        /// <summary>
        /// Indicates if properties are always read lazily.
        /// </summary>
        public bool Lazy => this.DeserializationOption == FlatBufferDeserializationOption.Lazy;

        /// <summary>
        /// Indicates if write through is supported.
        /// </summary>
        public bool SupportsWriteThrough => this.DeserializationOption == FlatBufferDeserializationOption.Lazy ||
                                            this.DeserializationOption == FlatBufferDeserializationOption.VectorCacheMutable;

        /// <summary>
        /// Indicates if the object is immutable OR changes to the object are guaranteed to be written back to the buffer.
        /// </summary>
        /// <remarks>
        /// VectorCacheMutable is not eligible here, since only some changes are written back.
        /// </remarks>
        public bool CanSerializeWithMemoryCopy => this.DeserializationOption == FlatBufferDeserializationOption.Lazy ||
                                                  this.DeserializationOption == FlatBufferDeserializationOption.PropertyCache;

        /// <summary>
        /// Indicates if FlatSharp should intercept app domain load events to look for cross-referenced generated assemblies. 
        /// Mostly useful for FlatSharp unit tests.
        /// </summary>
        public bool EnableAppDomainInterceptOnAssemblyLoad { get; set; }

        /// <summary>
        /// Allows FlatSharp to deserialize value structs with MemoryMarshal calls when on Little Endian
        /// architectures. Setting this to false will disable all MemoryMarshal calls, but a value of true
        /// is not wholly sufficient to enable them.
        /// </summary>
        public bool EnableValueStructMemoryMarshalDeserialization { get; set; } = true;

        /// <summary>
        /// Indicates if "protected internal" modifiers should be converted to protected.
        /// </summary>
        internal bool ConvertProtectedInternalToProtected { get; set; } = true;

        /// <summary>
        /// Specify the offset size
        /// </summary>
        public int? OffsetSize { get; set; } = null;

        /// <summary>
        /// Specify the size of the file identifier.
        /// </summary>
        public int? FileIdentifierSize { get; set; } = null;
        
        /// <summary>
        /// Specify if the size of the file identifier is strict.
        /// </summary>
        public bool? StrictFileIdentifierSize { get; set; } = null;
    }
}
