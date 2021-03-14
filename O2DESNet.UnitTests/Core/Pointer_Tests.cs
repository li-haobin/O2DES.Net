using NUnit.Framework;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace O2DESNet.UnitTests.Core
{
    public class Pointer_Tests
    {
        [Test]
        public void Creating_Empty_Pointer()
        {
            var pointer = Pointer.Empty;

            Assert.AreEqual(pointer.X, 0d);
            Assert.AreEqual(pointer.Y, 0d);
            Assert.AreEqual(pointer.Angle, 0d);
            Assert.AreEqual(pointer.Flipped, false);
            Assert.AreEqual(pointer.IsEmpty, true);
        }

        [Test]
        public void Created_Pointer_Should_Not_Empty()
        {
            Pointer pointer = new Pointer(1, 2, 45, true);
            Assert.AreEqual(pointer.IsEmpty, false);
        }

        [Test]
        public void PointerA_Should_Equal_PointerB()
        {
            Pointer pointerA = new Pointer(1, 2, 45, true);
            Pointer pointerB = new Pointer(1, 2, 45, true);
            Assert.AreEqual(pointerA, pointerB);
        }

        [Test]
        public void PointerA_Should_Not_Equal_PointerB()
        {
            Pointer pointerA = new Pointer(1, 2, 45, true);
            Pointer pointerB = new Pointer(1, 2, 45, false);
            Assert.AreNotEqual(pointerA, pointerB);

            pointerA = new Pointer(1, 2, 45, true);
            pointerB = new Pointer(2, 2, 45, true);
            Assert.AreNotEqual(pointerA, pointerB);

            pointerA = new Pointer(1, 2, 45, true);
            pointerB = new Pointer(1, 3, 45, true);
            Assert.AreNotEqual(pointerA, pointerB);

            pointerA = new Pointer(1, 2, 45, true);
            pointerB = new Pointer(1, 2, 46, true);
            Assert.AreNotEqual(pointerA, pointerB);
        }

        [Test]
        public void PointerA_Times_PointerB_Deivide_By_PointerB_Should_Return_PointerA()
        {
            Pointer pointerA = new Pointer(1, 2, 45, true);
            Pointer pointerB = new Pointer(2, 3, 45, true);

            var mulPointer = pointerA * pointerB;
            var divpointer = mulPointer / pointerB;

            Assert.AreEqual(divpointer, pointerA);
        }

        [Test]
        public void Created_Pointer_Can_Be_Serialized_As_Json_And_Deserialized_Back()
        {
            Pointer pointerA = new Pointer(1, 2, 45, true);

            // this option is just to make the json formatting pretty
            // and able to convert Pointer struct using custom converter
            var jsonOption = new JsonSerializerOptions { WriteIndented = true };
            jsonOption.Converters.Add(new PointerJsonConverter());

            // first, serialize Pointer as json string
            var json = JsonSerializer.Serialize(pointerA, jsonOption);
            TestContext.WriteLine("Output pointerA as Json");
            TestContext.WriteLine(json);

            // second, deserialize json string back to pointer
            var pointerB = JsonSerializer.Deserialize<Pointer>(json, jsonOption);

            // the result should be equal Pointer
            Assert.AreEqual(pointerA, pointerB);
        }
    }


    /// <summary>
    /// This is just a helper class for pointer testing. As pointer is a custom struct value type,
    /// there is no converter in Json serializer and it require custom converter to convert it.
    /// </summary>
    /// <seealso cref="System.Text.Json.Serialization.JsonConverter{O2DESNet.Pointer}" />
    internal class PointerJsonConverter : JsonConverter<Pointer>
    {
        public override Pointer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("JSON payload expected to start with StartObject token.");

            double X = 0, Y = 0, Angle = 0;
            bool Flipped = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.GetString() == nameof(X)) { reader.Read(); X = reader.GetDouble(); continue; }
                    if (reader.GetString() == nameof(Y)) { reader.Read(); Y = reader.GetDouble(); continue; }
                    if (reader.GetString() == nameof(Angle)) { reader.Read(); Angle = reader.GetDouble(); continue; }
                    if (reader.GetString() == nameof(Flipped)) { reader.Read(); Flipped = reader.GetBoolean(); continue; }
                }
            }

            return new Pointer(X, Y, Angle, Flipped);
        }

        public override void Write(Utf8JsonWriter writer, Pointer value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(value.X));
            writer.WriteNumberValue(value.X);
            writer.WritePropertyName(nameof(value.Y));
            writer.WriteNumberValue(value.Y);
            writer.WritePropertyName(nameof(value.Angle));
            writer.WriteNumberValue(value.Angle);
            writer.WritePropertyName(nameof(value.Flipped));
            writer.WriteBooleanValue(value.Flipped);
            writer.WritePropertyName(nameof(value.IsEmpty));
            writer.WriteBooleanValue(value.IsEmpty);
            writer.WriteEndObject();
            writer.Flush();
        }
    }
}
