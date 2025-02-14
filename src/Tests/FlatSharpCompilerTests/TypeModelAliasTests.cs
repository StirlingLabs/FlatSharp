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

namespace FlatSharpTests.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using FlatSharp;
    using FlatSharp.Attributes;
    using FlatSharp.Compiler;
    using FlatSharp.TypeModel;
    using Xunit;

    
    public class TypeModelAliasTests
    {
        [Fact]
        public void Alias_String()
            => this.AssertResolve("string", typeof(string), typeof(StringTypeModel));

        [Fact]
        public void Alias_Bool()
            => this.AssertResolve("bool", typeof(bool), typeof(BoolTypeModel));

        [Fact]
        public void Alias_Byte()
            => this.AssertResolve("byte", typeof(sbyte), typeof(SByteTypeModel));

        [Fact]
        public void Alias_Int8()
            => this.AssertResolve("int8", typeof(sbyte), typeof(SByteTypeModel));

        [Fact]
        public void Alias_UByte()
            => this.AssertResolve("ubyte", typeof(byte), typeof(ByteTypeModel));

        [Fact]
        public void Alias_UInt8()
            => this.AssertResolve("uint8", typeof(byte), typeof(ByteTypeModel));

        [Fact]
        public void Alias_Short()
            => this.AssertResolve("short", typeof(short), typeof(ShortTypeModel));

        [Fact]
        public void Alias_Int16()
            => this.AssertResolve("int16", typeof(short), typeof(ShortTypeModel));

        [Fact]
        public void Alias_UShort()
            => this.AssertResolve("ushort", typeof(ushort), typeof(UShortTypeModel));

        [Fact]
        public void Alias_UInt16()
            => this.AssertResolve("uint16", typeof(ushort), typeof(UShortTypeModel));

        [Fact]
        public void Alias_Int()
            => this.AssertResolve("int", typeof(int), typeof(IntTypeModel));

        [Fact]
        public void Alias_Int32()
            => this.AssertResolve("int32", typeof(int), typeof(IntTypeModel));

        [Fact]
        public void Alias_UInt()
            => this.AssertResolve("uint", typeof(uint), typeof(UIntTypeModel));

        [Fact]
        public void Alias_UInt32()
            => this.AssertResolve("uint32", typeof(uint), typeof(UIntTypeModel));

        [Fact]
        public void Alias_Long()
            => this.AssertResolve("long", typeof(long), typeof(LongTypeModel));

        [Fact]
        public void Alias_Int64()
            => this.AssertResolve("int64", typeof(long), typeof(LongTypeModel));

        [Fact]
        public void Alias_ULong()
            => this.AssertResolve("ulong", typeof(ulong), typeof(ULongTypeModel));

        [Fact]
        public void Alias_UInt64()
            => this.AssertResolve("uint64", typeof(ulong), typeof(ULongTypeModel));

        [Fact]
        public void Alias_Float32()
            => this.AssertResolve("float32", typeof(float), typeof(FloatTypeModel));

        [Fact]
        public void Alias_Float()
            => this.AssertResolve("float", typeof(float), typeof(FloatTypeModel));

        [Fact]
        public void Alias_Float64()
            => this.AssertResolve("float64", typeof(double), typeof(DoubleTypeModel));

        [Fact]
        public void Alias_Double()
            => this.AssertResolve("double", typeof(double), typeof(DoubleTypeModel));

        [Fact]
        public void Alias_Invalid()
        {
            TypeModelContainer container = TypeModelContainer.CreateDefault();
            Assert.False(container.TryResolveFbsAlias("foo", out _));
            Assert.False(container.TryResolveFbsAlias("Double", out _));
            Assert.False(container.TryResolveFbsAlias("", out _));
            Assert.False(container.TryResolveFbsAlias(null, out _));
        }

        private void AssertResolve(string alias, Type clrType, Type typeModelType)
        {
            TypeModelContainer container = TypeModelContainer.CreateDefault();
            Assert.True(container.TryResolveFbsAlias(alias, out ITypeModel? typeModel));

            Assert.IsType(typeModelType, typeModel);
            Assert.Equal(clrType, typeModel.ClrType);
        }
    }
}
