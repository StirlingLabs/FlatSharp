﻿/*
 * Copyright 2021 James Courtney
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

namespace FlatSharpTests.Compiler
{
    using System;
    using System.Reflection;
    using FlatSharp;
    using FlatSharp.Compiler;
    using Xunit;

    
    public class ApacheArrowTest
    {
        [Fact]
        public void Arrow_Compilation()
        {
            foreach (FlatBufferDeserializationOption item in Enum.GetValues(typeof(FlatBufferDeserializationOption)))
            {
                this.Compile_Arrow(item);
            }
        }

        private void Compile_Arrow(FlatBufferDeserializationOption option)
        {
            string schema = $@"
    // Licensed to the Apache Software Foundation (ASF) under one
    // or more contributor license agreements.  See the NOTICE file
    // distributed with this work for additional information
    // regarding copyright ownership.  The ASF licenses this file
    // to you under the Apache License, Version 2.0 (the
    // ""License""); you may not use this file except in compliance
    // with the License.  You may obtain a copy of the License at
    //
    //   http://www.apache.org/licenses/LICENSE-2.0
    //
    // Unless required by applicable law or agreed to in writing,
    // software distributed under the License is distributed on an
    // ""AS IS"" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
    // KIND, either express or implied.  See the License for the
    // specific language governing permissions and limitations
    // under the License.

    /// Logical types, vector layouts, and schemas

    namespace org.apache.arrow.flatbuf;

    enum MetadataVersion : short
    {{
        /// 0.1.0 (October 2016).
        V1,

        /// 0.2.0 (February 2017). Non-backwards compatible with V1.
        V2,

        /// 0.3.0 -> 0.7.1 (May - December 2017). Non-backwards compatible with V2.
        V3,

        /// >= 0.8.0 (December 2017). Non-backwards compatible with V3.
        V4,

        /// >= 1.0.0 (July 2020. Backwards compatible with V4 (V5 readers can read V4
        /// metadata and IPC messages). Implementations are recommended to provide a
        /// V4 compatibility mode with V5 format changes disabled.
        ///
        /// Incompatible changes between V4 and V5:
        /// - Union buffer layout has changed. In V5, Unions don't have a validity
        ///   bitmap buffer.
        V5,
    }}

    /// Represents Arrow Features that might not have full support
    /// within implementations. This is intended to be used in
    /// two scenarios:
    ///  1.  A mechanism for readers of Arrow Streams
    ///      and files to understand that the stream or file makes
    ///      use of a feature that isn't supported or unknown to
    ///      the implementation (and therefore can meet the Arrow
    ///      forward compatibility guarantees).
    ///  2.  A means of negotiating between a client and server
    ///      what features a stream is allowed to use. The enums
    ///      values here are intented to represent higher level
    ///      features, additional details maybe negotiated
    ///      with key-value pairs specific to the protocol.
    ///
    /// Enums added to this list should be assigned power-of-two values
    /// to facilitate exchanging and comparing bitmaps for supported
    /// features.
    enum Feature : long
    {{
        /// Needed to make flatbuffers happy.
        UNUSED = 0,
        /// The stream makes use of multiple full dictionaries with the
        /// same ID and assumes clients implement dictionary replacement
        /// correctly.
        DICTIONARY_REPLACEMENT = 1,
        /// The stream makes use of compressed bodies as described
        /// in Message.fbs.
        COMPRESSED_BODY = 2
    }}

    /// These are stored in the flatbuffer in the Type union below

    table Null
    {{
    }}

    /// A Struct_ in the flatbuffer metadata is the same as an Arrow Struct
    /// (according to the physical memory layout). We used Struct_ here as
    /// Struct is a reserved word in Flatbuffers
    table Struct_
    {{
    }}

    table List
    {{
    }}

    /// Same as List, but with 64-bit offsets, allowing to represent
    /// extremely large data values.
    table LargeList
    {{
    }}

    table FixedSizeList
    {{
        /// Number of list items per value
        listSize: int;
    }}

    /// A Map is a logical nested type that is represented as
    ///
    /// List<entries: Struct<key: K, value: V>>
    ///
    /// In this layout, the keys and values are each respectively contiguous. We do
    /// not constrain the key and value types, so the application is responsible
    /// for ensuring that the keys are hashable and unique. Whether the keys are sorted
    /// may be set in the metadata for this field.
    ///
    /// In a field with Map type, the field has a child Struct field, which then
    /// has two children: key type and the second the value type. The names of the
    /// child fields may be respectively ""entries"", ""key"", and ""value"", but this is
    /// not enforced.
    ///
    /// Map
    /// ```text
    ///   - child[0] entries: Struct
    ///     - child[0] key: K
    ///     - child[1] value: V
    /// ```
    /// Neither the ""entries"" field nor the ""key"" field may be nullable.
    ///
    /// The metadata is structured so that Arrow systems without special handling
    /// for Map can make Map an alias for List. The ""layout"" attribute for the Map
    /// field must have the same contents as a List.
    table Map
    {{
        /// Set to true if the keys within each value are sorted
        keysSorted: bool;
    }}

    enum UnionMode : short {{ Sparse, Dense }}

    /// A union is a complex type with children in Field
    /// By default ids in the type vector refer to the offsets in the children
    /// optionally typeIds provides an indirection between the child offset and the type id
    /// for each child `typeIds[offset]` is the id used in the type vector
    table Union
    {{
        mode: UnionMode;
        typeIds: [ int ]; // optional, describes typeid of each child.
    }}

    table Int
    {{
        bitWidth: int; // restricted to 8, 16, 32, and 64 in v1
        is_signed: bool;
    }}

    enum Precision : short {{ HALF, SINGLE, DOUBLE }}

    table FloatingPoint
    {{
        precision: Precision;
    }}

    /// Unicode with UTF-8 encoding
    table Utf8
    {{
    }}

    /// Opaque binary data
    table Binary
    {{
    }}

    /// Same as Utf8, but with 64-bit offsets, allowing to represent
    /// extremely large data values.
    table LargeUtf8
    {{
    }}

    /// Same as Binary, but with 64-bit offsets, allowing to represent
    /// extremely large data values.
    table LargeBinary
    {{
    }}

    table FixedSizeBinary
    {{
        /// Number of bytes per value
        byteWidth: int;
    }}

    table Bool
    {{
    }}

    /// Exact decimal value represented as an integer value in two's
    /// complement. Currently only 128-bit (16-byte) and 256-bit (32-byte) integers
    /// are used. The representation uses the endianness indicated
    /// in the Schema.
    table Decimal
    {{
        /// Total number of decimal digits
        precision: int;

        /// Number of digits after the decimal point "".""
        scale: int;

        /// Number of bits per value. The only accepted widths are 128 and 256.
        /// We use bitWidth for consistency with Int::bitWidth.
        bitWidth: int = 128;
    }}

    enum DateUnit : short
    {{
        DAY,
        MILLISECOND
    }}

    /// Date is either a 32-bit or 64-bit type representing elapsed time since UNIX
    /// epoch (1970-01-01), stored in either of two units:
    ///
    /// * Milliseconds (64 bits) indicating UNIX time elapsed since the epoch (no
    ///   leap seconds), where the values are evenly divisible by 86400000
    /// * Days (32 bits) since the UNIX epoch
    table Date
    {{
        unit: DateUnit = MILLISECOND;
    }}

    enum TimeUnit : short {{ SECOND, MILLISECOND, MICROSECOND, NANOSECOND }}

    /// Time type. The physical storage type depends on the unit
    /// - SECOND and MILLISECOND: 32 bits
    /// - MICROSECOND and NANOSECOND: 64 bits
    table Time
    {{
        unit: TimeUnit = MILLISECOND;
        bitWidth: int = 32;
    }}

    /// Time elapsed from the Unix epoch, 00:00:00.000 on 1 January 1970, excluding
    /// leap seconds, as a 64-bit integer. Note that UNIX time does not include
    /// leap seconds.
    ///
    /// The Timestamp metadata supports both ""time zone naive"" and ""time zone
    /// aware"" timestamps. Read about the timezone attribute for more detail
    table Timestamp
    {{
        unit: TimeUnit;

        /// The time zone is a string indicating the name of a time zone, one of:
        ///
        /// * As used in the Olson time zone database (the ""tz database"" or
        ///   ""tzdata""), such as ""America/New_York""
        /// * An absolute time zone offset of the form +XX:XX or -XX:XX, such as +07:30
        ///
        /// Whether a timezone string is present indicates different semantics about
        /// the data:
        ///
        /// * If the time zone is null or equal to an empty string, the data is ""time
        ///   zone naive"" and shall be displayed *as is* to the user, not localized
        ///   to the locale of the user. This data can be though of as UTC but
        ///   without having ""UTC"" as the time zone, it is not considered to be
        ///   localized to any time zone
        ///
        /// * If the time zone is set to a valid value, values can be displayed as
        ///   ""localized"" to that time zone, even though the underlying 64-bit
        ///   integers are identical to the same data stored in UTC. Converting
        ///   between time zones is a metadata-only operation and does not change the
        ///   underlying values
        timezone: string;
    }}

    enum IntervalUnit : short {{ YEAR_MONTH, DAY_TIME }}
    // A ""calendar"" interval which models types that don't necessarily
    // have a precise duration without the context of a base timestamp (e.g.
    // days can differ in length during day light savings time transitions).
    // YEAR_MONTH - Indicates the number of elapsed whole months, stored as
    //   4-byte integers.
    // DAY_TIME - Indicates the number of elapsed days and milliseconds,
    //   stored as 2 contiguous 32-bit integers (8-bytes in total).  Support
    //   of this IntervalUnit is not required for full arrow compatibility.
    table Interval
    {{
        unit: IntervalUnit;
    }}

    // An absolute length of time unrelated to any calendar artifacts.
    //
    // For the purposes of Arrow Implementations, adding this value to a Timestamp
    // (""t1"") naively (i.e. simply summing the two number) is acceptable even
    // though in some cases the resulting Timestamp (t2) would not account for
    // leap-seconds during the elapsed time between ""t1"" and ""t2"".  Similarly,
    // representing the difference between two Unix timestamp is acceptable, but
    // would yield a value that is possibly a few seconds off from the true elapsed
    // time.
    //
    //  The resolution defaults to millisecond, but can be any of the other
    //  supported TimeUnit values as with Timestamp and Time types.  This type is
    //  always represented as an 8-byte integer.
    table Duration
    {{
        unit: TimeUnit = MILLISECOND;
    }}

    /// ----------------------------------------------------------------------
    /// Top-level Type value, enabling extensible type-specific metadata. We can
    /// add new logical types to Type without breaking backwards compatibility

    union Type
    {{
        Null,
        Int,
        FloatingPoint,
        Binary,
        Utf8,
        Bool,
        Decimal,
        Date,
        Time,
        Timestamp,
        Interval,
        List,
        Struct_,
        Union,
        FixedSizeBinary,
        FixedSizeList,
        Map,
        Duration,
        LargeBinary,
        LargeUtf8,
        LargeList,
    }}

    /// ----------------------------------------------------------------------
    /// user defined key value pairs to add custom metadata to arrow
    /// key namespacing is the responsibility of the user

    table KeyValue
    {{
        key: string;
        value: string;
    }}

    /// ----------------------------------------------------------------------
    /// Dictionary encoding metadata
    /// Maintained for forwards compatibility, in the future
    /// Dictionaries might be explicit maps between integers and values
    /// allowing for non-contiguous index values
    enum DictionaryKind : short {{ DenseArray }}
    table DictionaryEncoding
    {{
        /// The known dictionary id in the application where this data is used. In
        /// the file or streaming formats, the dictionary ids are found in the
        /// DictionaryBatch messages
        id: long;

        /// The dictionary indices are constrained to be non-negative integers. If
        /// this field is null, the indices must be signed int32. To maximize
        /// cross-language compatibility and performance, implementations are
        /// recommended to prefer signed integer types over unsigned integer types
        /// and to avoid uint64 indices unless they are required by an application.
        indexType: Int;

        /// By default, dictionaries are not ordered, or the order does not have
        /// semantic meaning. In some statistical, applications, dictionary-encoding
        /// is used to represent ordered categorical data, and we provide a way to
        /// preserve that metadata here
        isOrdered: bool;

        dictionaryKind: DictionaryKind;
    }}

    /// ----------------------------------------------------------------------
    /// A field represents a named column in a record / row batch or child of a
    /// nested type.

    table Field
    {{
        /// Name is not required, in i.e. a List
        name: string;

        /// Whether or not this field can contain nulls. Should be true in general.
        nullable: bool;

        /// This is the type of the decoded value if the field is dictionary encoded.
        type: Type;

        /// Present only if the field is dictionary encoded.
        dictionary: DictionaryEncoding;

        /// children apply only to nested data types like Struct, List and Union. For
        /// primitive types children will have length 0.
        children: [Field];

        /// User-defined metadata
        custom_metadata: [KeyValue];
    }}

    /// ----------------------------------------------------------------------
    /// Endianness of the platform producing the data

    enum Endianness : short {{ Little, Big }}

    /// ----------------------------------------------------------------------
    /// A Buffer represents a single contiguous memory segment
    struct Buffer
    {{
        /// The relative offset into the shared memory page where the bytes for this
        /// buffer starts
        offset: long;

        /// The absolute length (in bytes) of the memory buffer. The memory is found
        /// from offset (inclusive) to offset + length (non-inclusive). When building
        /// messages using the encapsulated IPC message, padding bytes may be written
        /// after a buffer, but such padding bytes do not need to be accounted for in
        /// the size here.
        length: long;
    }}

    /// ----------------------------------------------------------------------
    /// A Schema describes the columns in a row batch

    table Schema (fs_serializer)
    {{

        /// endianness of the buffer
        /// it is Little Endian by default
        /// if endianness doesn't match the underlying system then the vectors need to be converted
        endianness: Endianness=Little;

        fields: [Field];
        // User-defined metadata
        custom_metadata: [KeyValue];

        /// Features used in the stream/file.
        features : [Feature];
    }}

    root_type Schema;";

            Assembly asm = FlatSharpCompiler.CompileAndLoadAssembly(schema, new());
        }
    }
}