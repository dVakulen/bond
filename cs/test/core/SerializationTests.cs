﻿namespace UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Bond;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    // Name conflicts with Bond.Tag
    public abstract class wstring { }
    public abstract class blob { }
    public abstract class nullable<T> { }
    public abstract class bonded<T> { }

    public static class BondTypeAliasConverter
    {
        public static long Convert(DateTime value, long unused)
        {
            return value.Ticks;
        }

        public static DateTime Convert(long value, DateTime unused)
        {
            if (value >= DateTime.MinValue.Ticks && value <= DateTime.MaxValue.Ticks)
                return new DateTime(value);

            return default(DateTime);
        }
    }

    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void BasicTypes()
        {
            TestSerialization<BasicTypes>();
        }

        [TestMethod]
        public void IntegerLimits()
        {
            var limits = new[] { 
                new BasicTypes 
                {
                    _int8 = sbyte.MaxValue, 
                    _int16 = short.MaxValue,
                    _int32 = int.MaxValue,
                    _int64 = long.MaxValue,
                    _uint8 = byte.MaxValue,
                    _uint16 = ushort.MaxValue,
                    _uint32 = uint.MaxValue,
                    _uint64 = ulong.MaxValue
                },
                new BasicTypes {
                    _int8 = sbyte.MinValue,
                    _int16 = short.MinValue,
                    _int32 = int.MinValue,
                    _int64 = long.MinValue,
                    _uint8 = byte.MinValue,
                    _uint16 = ushort.MinValue,
                    _uint32 = uint.MinValue,
                    _uint64 = ulong.MinValue
                }};

            Util.RoundtripMemory<BasicTypes, BasicTypes> memoryRoundtrip = (serialize, deserialize) =>
            {
                foreach (var from in limits)
                {
                    var data = serialize(from);
                    var to = deserialize(data);
                    Assert.IsTrue(from.IsEqual<BasicTypes>(to));
                }
            };

            memoryRoundtrip(Util.SerializeUnsafeCB, Util.DeserializeSafeCB<BasicTypes>);
            memoryRoundtrip(Util.SerializeUnsafeCB, Util.DeserializeUnsafeCB<BasicTypes>);
            memoryRoundtrip(Util.SerializeSP, Util.DeserializeSafeSP<BasicTypes, BasicTypes>);
            memoryRoundtrip(Util.SerializeSP, Util.DeserializeUnsafeSP<BasicTypes, BasicTypes>);
        }

        [TestMethod]
        public void ReadonlyBasicTypes()
        {
            TestSerialization<Readonly.BasicTypes>();
        }

        [TestMethod]
        public void ReadonlySimpleContainers()
        {
            TestSerialization<Readonly.SimpleContainers>();
        }

        [TestMethod]
        public void Nested()
        {
            TestSerialization<Nested>();
        }

        [TestMethod]
        public void Void()
        {
            TestSerialization<Bond.Void>();
        }

        [TestMethod]
        public void Lists()
        {
            TestSerialization<Lists>();
        }

        [TestMethod]
        public void Vectors()
        {
            TestSerialization<Vectors>();
        }

        [TestMethod]
        public void Sets()
        {
            TestSerialization<Sets>();
        }

        [TestMethod]
        public void Maps()
        {
            TestSerialization<Maps>();
        }

        [TestMethod]
        public void Containers()
        {
            TestSerialization<BondClass<SortedSet<string>>>();
            TestSerialization<Containers>();
        }

        [TestMethod]
        public void NestedContainers()
        {
            TestSerialization<NestedContainers>();
        }

        [TestMethod]
        public void Inheritance()
        {
            TestSerialization<Deep>();
            TestSerialization<Derived>();
        }

        [TestMethod]
        public void Inheritance_SliceToBase()
        {
            TestSerialization<Derived, Nested>();
            TestSerialization<Derived, EmptyBase>();
        }

        [TestMethod]
        public void Inheritance_SliceFieldToBase()
        {
            TestFieldSerialization<EmptyBase, Nested>();
            TestFieldSerialization<DerivedView, EmptyBase>();
        }

        [TestMethod]
        public void Nothing()
        {
            TestSerialization<Nothing>();
            TestSerialization<NotNothingView, Nothing>();
        }
        
        [TestMethod]
        public void NullableBasicTypes()
        {
            TestSerialization<NullableBasicTypes>();
        }

        [TestMethod]
        public void NullableStruct()
        {
            TestSerialization<NullableStruct>();
        }

        public void TypeMismatch<From, To>()
        {
            Util.RoundtripStream<BondClass<From>, BondClass<To>> test = (serialize, deserialize) =>
            {
                try
                {
                    var stream = new MemoryStream();
                    serialize(new BondClass<From>(), stream);
                    stream.Position = 0;
                    deserialize(stream);
                    Assert.Fail("Deserialization of mismatched type didn't throw exception.");
                }
                catch (InvalidDataException)
                {}
            };

            // TODO: for untagged protocol mismatch schema will be detected in schema validation
            test(Util.SerializeCB, Util.DeserializeCB<BondClass<To>>);
        }

        [TestMethod]
        public void TypeMismatch()
        {
            TypeMismatch<List<int>, List<HashSet<int>>>();
            TypeMismatch<List<int>, HashSet<int>>();
            TypeMismatch<List<int>, int>();
            TypeMismatch<List<int>, BasicTypes>();
            TypeMismatch<BasicTypes, List<int>>();
            TypeMismatch<BasicTypes, double>();
            TypeMismatch<BasicTypes, HashSet<int>>();
            TypeMismatch<HashSet<int>, HashSet<string>>();
            TypeMismatch<HashSet<int>, List<float>>();
            TypeMismatch<HashSet<int>, BasicTypes>();
            TypeMismatch<Dictionary<int, int>, Dictionary<int, string>>();
            TypeMismatch<Dictionary<int, int>, Dictionary<string, int>>();
            TypeMismatch<Dictionary<int, int>, HashSet<string>>();
        }

        [TestMethod]
        public void ContainersOfNullable()
        {
            TestSerialization<ContainersOfNullable>();

            var from = new ContainersOfNullable
            {
                vn = new VectorsOfNullable
                {
                    _bool = {false, null, true},
                    _double = {null, null},
                    _str = {"foo", null},
                    _wstr = {null, "bar"},
                    basic = {null},
                    _uint16 = {0, null, null, 1},
                    _blob =
                    {
                        default(ArraySegment<byte>), 
                        new ArraySegment<byte>(new byte[] { 0, 1 }), 
                        default(ArraySegment<byte>)
                    }
                },
                mn = new MapsOfNullable
                {
                    _basic = {{"foo", null}},
                    _double = {{"bar", 3.14}, {"foo", null}},
                    _float = {{"bar", null}, {"foo", 3.14f}},
                    _blob =
                    {
                        {"bar", default(ArraySegment<byte>)}, 
                        {"foo", new ArraySegment<byte>(new byte[] { 0, 1 })},
                        {"bux", default(ArraySegment<byte>) }
                    }
                }
            };

            Util.AllSerializeDeserialize<ContainersOfNullable, ContainersOfNullable>(from);
        }
        
        [TestMethod]
        public void NullableContainers()
        {
            TestSerialization<NullableContainers>();
        }

        [TestMethod]
        public void Blobs()
        {
            TestSerialization<StructWithBlobs>();
            TestSerialization<StructWithBlobs, StructWithByteLists>();
            TestSerialization<StructWithByteLists, StructWithBlobs>();
        }

        [TestMethod]
        public void Recursive()
        {
            TestSerialization<Tree>();
        }

        [TestMethod]
        public void TypePromotion()
        {
            TestTypePromotion<byte, UInt16>();
            TestTypePromotion<byte, UInt32>();
            TestTypePromotion<byte, UInt64>();
            TestTypePromotion<UInt16, UInt32>();
            TestTypePromotion<UInt16, UInt64>();
            TestTypePromotion<UInt32, UInt64>();
            TestTypePromotion<sbyte, Int16>();
            TestTypePromotion<sbyte, Int32>();
            TestTypePromotion<sbyte, Int64>();
            TestTypePromotion<Int16, Int32>();
            TestTypePromotion<Int16, Int64>();
            TestTypePromotion<Int32, Int64>();
            TestTypePromotion<float, double>();

            TestTypePromotion<byte, byte?>();
            TestTypePromotion<Int16, Int16?>();
            TestTypePromotion<Int32, Int32?>();
        }

        [TestMethod]
        public void Views()
        {
            TestSerialization<BasicTypes, BasicTypesView>();
            TestFieldSerialization<BasicTypes, BasicTypesView>();
        }


        [TestMethod]
        public void Omit()
        {
            // Omit optional fields set to default value
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new StructWithDefaults(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new ContainersOfNullable(), 5);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new NullableVectors(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new NullableLists(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new NullableBasicTypes(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Lists(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Vectors(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Sets(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Maps(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new StructWithBlobs(), 1);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Nothing(), 1);
            
            // Don't skip empty container for field with default nothing
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Nothing { b = new ArraySegment<byte>(new byte[0]) }, 5);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Nothing { l = new LinkedList<string>() }, 5);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Nothing { s = new HashSet<double>() }, 5);
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new Nothing { m = new Dictionary<string, double>() }, 6);
            
            // Don't omit required fields
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new BondClass<Int32>(), 3);

            // Don't omit required_optional fields
            TestPayloadSize(Util.SerializeCB, Util.TranscodeCBCB, new RequiredOptional(), 4);
        }

        [TestMethod]
        public void Skip()
        {
            TestFieldSkip<bool>();
            TestFieldSkip<byte>();
            TestFieldSkip<UInt16>();
            TestFieldSkip<UInt32>();
            TestFieldSkip<UInt64>();
            TestFieldSkip<sbyte>();
            TestFieldSkip<Int16>();
            TestFieldSkip<Int32>();
            TestFieldSkip<Int64>();
            TestFieldSkip<float>();
            TestFieldSkip<double>();
            TestFieldSkip<string>();

            TestFieldSkip<BasicTypes>();
            TestFieldSkip<Nested>();
            TestFieldSkip<Containers>();
            TestFieldSkip<NestedContainers>();
            TestFieldSkip<Derived>();
            TestFieldSkip<StructWithBlobs>();
            TestFieldSkip<Tree>();
        }

        void TestRequired<From, To>()
        {
            var from = Random.Init<From>();
            Util.RoundtripStream<From, To> testRequired = (serialize, deserialize) =>
            {
                var stream = new MemoryStream();

                serialize(from, stream);
                stream.Position = 0;
                
                try
                {
                    deserialize(stream);
                    Assert.Fail("Missing required field not detected.");
                }
                catch (InvalidDataException e)
                {
                    Assert.IsTrue(e.Message.Contains("Required field"), e.Message);
                }
            };

            testRequired(Util.SerializeCB, Util.DeserializeCB<To>);
            // TODO: tagged protocol determine missing required fields during schema valiadation
            //testRequired(Util.SerializeSP, Util.DeserializeSP<From, To>);
        }

        [TestMethod]
        public void Required()
        {
            TestSerialization<Required>();
            TestSerialization<RequiredInDerived>();
            TestSerialization<RequiredInBase>();
            TestSerialization<RequiredInBaseAndDerived>();
            TestSerialization<Optional, RequiredOptional>();
            TestRequired<BondClass<double>, BondClass<int, double>>();
            TestRequired<BondClass<double>, BondClass<BasicTypes, double>>();
        }

        [TestMethod]
        public void Box()
        {
            TestSerialization<Bond.Box<bool>>();
            TestSerialization<Bond.Box<sbyte>>();
            TestSerialization<Bond.Box<short>>();
            TestSerialization<Bond.Box<int>>();
            TestSerialization<Bond.Box<long>>();
            TestSerialization<Bond.Box<byte>>();
            TestSerialization<Bond.Box<ushort>>();
            TestSerialization<Bond.Box<uint>>();
            TestSerialization<Bond.Box<ulong>>();
            TestSerialization<Bond.Box<float>>();
            TestSerialization<Bond.Box<double>>();
        }


        private void TestTypePromotion<From, To>()
        {
            TestFieldSerialization<From, To>();
            TestFieldSerialization<List<From>, List<To>>();
            TestFieldSerialization<HashSet<From>, HashSet<To>>();
            TestFieldSerialization<Dictionary<From, string>, Dictionary<To, string>>();
            TestFieldSerialization<Dictionary<string, From>, Dictionary<string, To>>();
        }

        void TestFieldSkip<T>()
        {
            TestSerialization<BondClass<T, double>, BondClass<double>>(true);
            TestSerialization<BondClass<List<T>, double>, BondClass<double>>(true);
            TestSerialization<BondClass<HashSet<T>, double>, BondClass<double>>(true);
            TestSerialization<BondClass<Dictionary<string, T>, double>, BondClass<double>>(true);
        }

        void TestSerialization<From, To>(bool noTranscoding = false)
            where From : class, new()
            where To : class
        {
            Util.AllSerializeDeserialize<From, To>(new From(), noTranscoding);
            Util.AllSerializeDeserialize<From, To>(Random.Init<From>(), noTranscoding);
        }

        void TestSerialization<T>(bool noTranscoding = false)
            where T : class, new()
        {
            Assert.IsTrue(Comparer.Equal(new T(), new T()));
            TestSerialization<T, T>(noTranscoding);
        }

        void TestPayloadSize<T>(Action<T, Stream> serialize, Action<Stream, Stream> transcode, T obj, int size)
        {
            var stream = new MemoryStream();
            
            serialize(obj, stream);
            Assert.AreEqual(size, stream.Length);

            using (var stream2 = new MemoryStream())
            {
                stream.Position = 0;
                transcode(stream, stream2);
                Assert.AreEqual(size, stream2.Length);
            }
        }

        void TestFieldSerialization<From, To>()
        {
            TestSerialization<BondClass<From>, BondClass<To>>();

            if (BondClass<From>.TypeId != BondClass<To>.TypeId)
            {
                try
                {
                    TestSerialization<BondClass<To>, BondClass<From>>();
                }
                catch (InvalidDataException e)
                {
                    var expected = string.Format("Invalid type {0}, expected {1}",
                        BondClass<To>.TypeId, BondClass<From>.TypeId);
                    Assert.AreEqual(expected, e.Message);
                    return;
                }

                Assert.IsTrue(false);
            }
        }
    }
}
