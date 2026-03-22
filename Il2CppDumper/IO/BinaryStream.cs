using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppDumper
{
    public class BinaryStream : IDisposable
    {
        public double Version;
        public bool Is32Bit;
        public ulong ImageBase;
        private readonly Stream stream;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        private readonly MethodInfo readClass;
        private readonly MethodInfo readClassArray;
        private readonly Dictionary<Type, MethodInfo> genericMethodCache;
        private readonly Dictionary<FieldInfo, VersionAttribute[]> attributeCache;

        public BinaryStream(Stream input)
        {
            stream = input;
            reader = new BinaryReader(stream, Encoding.UTF8, true);
            writer = new BinaryWriter(stream, Encoding.UTF8, true);
            readClass = GetType().GetMethod("ReadClass", Type.EmptyTypes);
            readClassArray = GetType().GetMethod("ReadClassArray", new[] { typeof(long) });
            genericMethodCache = new();
            attributeCache = new();
        }

        public bool ReadBoolean() => reader.ReadBoolean();

        public byte ReadByte() => reader.ReadByte();

        public byte[] ReadBytes(int count) => reader.ReadBytes(count);

        public sbyte ReadSByte() => reader.ReadSByte();

        public short ReadInt16() => reader.ReadInt16();

        public ushort ReadUInt16() => reader.ReadUInt16();

        public int ReadInt32() => reader.ReadInt32();

        public uint ReadUInt32() => reader.ReadUInt32();

        public long ReadInt64() => reader.ReadInt64();

        public ulong ReadUInt64() => reader.ReadUInt64();

        public float ReadSingle() => reader.ReadSingle();

        public double ReadDouble() => reader.ReadDouble();

        public uint ReadCompressedUInt32() => reader.ReadCompressedUInt32();

        public int ReadCompressedInt32() => reader.ReadCompressedInt32();

        public uint ReadULeb128() => reader.ReadULeb128();

        public void Write(bool value) => writer.Write(value);

        public void Write(byte value) => writer.Write(value);

        public void Write(sbyte value) => writer.Write(value);

        public void Write(short value) => writer.Write(value);

        public void Write(ushort value) => writer.Write(value);

        public void Write(int value) => writer.Write(value);

        public void Write(uint value) => writer.Write(value);

        public void Write(long value) => writer.Write(value);

        public void Write(ulong value) => writer.Write(value);

        public void Write(float value) => writer.Write(value);

        public void Write(double value) => writer.Write(value);

        public ulong Position
        {
            get => (ulong)stream.Position;
            set => stream.Position = (long)value;
        }

        public ulong Length => (ulong)stream.Length;

        private object ReadPrimitive(Type type)
        {
            return type.Name switch
            {
                "Int32" => ReadInt32(),
                "UInt32" => ReadUInt32(),
                "Int16" => ReadInt16(),
                "UInt16" => ReadUInt16(),
                "Byte" => ReadByte(),
                "Int64" => ReadIntPtr(),
                "UInt64" => ReadUIntPtr(),
                _ => throw new NotSupportedException()
            };
        }

        public T ReadClass<T>(ulong addr) where T : new()
        {
            Position = addr;
            return ReadClass<T>();
        }

        public TypeIndex ReadTypeIndex()
        {
            if (Version < 35)
            {
                // Before Version 35, these were always Int32
                int value = ReadInt32();
                return new TypeIndex(value);
            }
            else
            {
                switch (Metadata.typeIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new TypeIndex(-1);
                            return new TypeIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new TypeIndex(-1);
                            return new TypeIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new TypeIndex(-1);
                            return new TypeIndex((int)value);
                        }
                }
            }
        }

        public TypeDefinitionIndex ReadTypeDefinitionIndex()
        {
            if (Version < 35)
            {
                // Before Version 35, these were always Int32
                int value = ReadInt32();
                return new TypeDefinitionIndex(value);
            }
            else
            {
                switch (Metadata.typeDefinitionIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new TypeDefinitionIndex(-1);
                            return new TypeDefinitionIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new TypeDefinitionIndex(-1);
                            return new TypeDefinitionIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new TypeDefinitionIndex(-1);
                            return new TypeDefinitionIndex((int)value);
                        }
                }
            }
        }
        public GenericContainerIndex ReadGenericContainerIndex()
        {
            if (Version < 35)
            {
                // Before Version 35, these were always Int32
                int value = ReadInt32();
                return new GenericContainerIndex(value);
            }
            else
            {
                switch (Metadata.genericContainerIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new GenericContainerIndex(-1);
                            return new GenericContainerIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new GenericContainerIndex(-1);
                            return new GenericContainerIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new GenericContainerIndex(-1);
                            return new GenericContainerIndex((int)value);
                        }
                }
            }
        }
        public ParameterIndex ReadParameterIndex()
        {
            if (Version < 39)
            {
                // Before Version 39, these were always Int32
                int value = ReadInt32();
                return new ParameterIndex(value);
            }
            else
            {
                switch (Metadata.parameterIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new ParameterIndex(-1);
                            return new ParameterIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new ParameterIndex(-1);
                            return new ParameterIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new ParameterIndex(-1);
                            return new ParameterIndex((int)value);
                        }
                }
            }
        }

        public MethodIndex ReadMethodIndex()
        {
            if (Version < 105)
            {
                int value = ReadInt32();
                return new MethodIndex(value);
            }
            else
            {
                switch (Metadata.methodIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new MethodIndex(-1);
                            return new MethodIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new MethodIndex(-1);
                            return new MethodIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new MethodIndex(-1);
                            return new MethodIndex((int)value);
                        }
                }
            }
        }

        public FieldIndex ReadFieldIndex()
        {
            if (Version < 106)
            {
                int value = ReadInt32();
                return new FieldIndex(value);
            }
            else
            {
                switch (Metadata.fieldIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new FieldIndex(-1);
                            return new FieldIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new FieldIndex(-1);
                            return new FieldIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new FieldIndex(-1);
                            return new FieldIndex((int)value);
                        }
                }
            }
        }

        public EventIndex ReadEventIndex()
        {
            if (Version < 104)
            {
                int value = ReadInt32();
                return new EventIndex(value);
            }
            else
            {
                switch (Metadata.eventIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new EventIndex(-1);
                            return new EventIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new EventIndex(-1);
                            return new EventIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new EventIndex(-1);
                            return new EventIndex((int)value);
                        }
                }
            }
        }

        public PropertyIndex ReadPropertyIndex()
        {
            if (Version < 104)
            {
                int value = ReadInt32();
                return new PropertyIndex(value);
            }
            else
            {
                switch (Metadata.propertyIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new PropertyIndex(-1);
                            return new PropertyIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new PropertyIndex(-1);
                            return new PropertyIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new PropertyIndex(-1);
                            return new PropertyIndex((int)value);
                        }
                }
            }
        }

        public NestedTypeIndex ReadNestedTypeIndex()
        {
            if (Version < 104)
            {
                int value = ReadInt32();
                return new NestedTypeIndex(value);
            }
            else
            {
                switch (Metadata.nestedTypeIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new NestedTypeIndex(-1);
                            return new NestedTypeIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new NestedTypeIndex(-1);
                            return new NestedTypeIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new NestedTypeIndex(-1);
                            return new NestedTypeIndex((int)value);
                        }
                }
            }
        }

        public InterfacesIndex ReadInterfacesIndex()
        {
            if (Version < 104)
            {
                int value = ReadInt32();
                return new InterfacesIndex(value);
            }
            else
            {
                switch (Metadata.interfacesIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new InterfacesIndex(-1);
                            return new InterfacesIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new InterfacesIndex(-1);
                            return new InterfacesIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new InterfacesIndex(-1);
                            return new InterfacesIndex((int)value);
                        }
                }
            }
        }

        public GenericParameterIndex ReadGenericParameterIndex()
        {
            if (Version < 106)
            {
                int value = ReadInt32();
                return new GenericParameterIndex(value);
            }
            else
            {
                switch (Metadata.genericParameterIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new GenericParameterIndex(-1);
                            return new GenericParameterIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new GenericParameterIndex(-1);
                            return new GenericParameterIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new GenericParameterIndex(-1);
                            return new GenericParameterIndex((int)value);
                        }
                }
            }
        }

        public DefaultValueDataIndex ReadDefaultValueDataIndex()
        {
            if (Version < 106)
            {
                int value = ReadInt32();
                return new DefaultValueDataIndex(value);
            }
            else
            {
                switch (Metadata.defaultValueDataIndexSize)
                {
                    case 1:
                        {
                            uint value = ReadByte();
                            if (value == Byte.MaxValue) return new DefaultValueDataIndex(-1);
                            return new DefaultValueDataIndex((int)value);
                        }
                    case 2:
                        {
                            uint value = ReadUInt16();
                            if (value == UInt16.MaxValue) return new DefaultValueDataIndex(-1);
                            return new DefaultValueDataIndex((int)value);
                        }
                    case 4:
                    default:
                        {
                            uint value = ReadUInt32();
                            if (value == UInt32.MaxValue) return new DefaultValueDataIndex(-1);
                            return new DefaultValueDataIndex((int)value);
                        }
                }
            }
        }

        public T ReadClass<T>() where T : new()
        {
            var type = typeof(T);
            if (type.IsPrimitive)
            {
                return (T)ReadPrimitive(type);
            }
            else
            {
                var t = new T();
                foreach (var i in t.GetType().GetFields())
                {
                    if (!attributeCache.TryGetValue(i, out var versionAttributes))
                    {
                        if (Attribute.IsDefined(i, typeof(VersionAttribute)))
                        {
                            versionAttributes = i.GetCustomAttributes<VersionAttribute>().ToArray();
                            attributeCache.Add(i, versionAttributes);
                        }
                    }
                    if (versionAttributes?.Length > 0)
                    {
                        var read = false;
                        foreach (var versionAttribute in versionAttributes)
                        {
                            if (Version >= versionAttribute.Min && Version <= versionAttribute.Max)
                            {
                                read = true;
                                break;
                            }
                        }
                        if (!read)
                        {
                            continue;
                        }
                    }
                    var fieldType = i.FieldType;
                    if (fieldType.IsPrimitive)
                    {
                        i.SetValue(t, ReadPrimitive(fieldType));
                    }
                    else if (fieldType.IsEnum)
                    {
                        var e = fieldType.GetField("value__").FieldType;
                        i.SetValue(t, ReadPrimitive(e));
                    }
                    else if (fieldType.IsArray)
                    {
                        var arrayLengthAttribute = i.GetCustomAttribute<ArrayLengthAttribute>();
                        if (!genericMethodCache.TryGetValue(fieldType, out var methodInfo))
                        {
                            methodInfo = readClassArray.MakeGenericMethod(fieldType.GetElementType());
                            genericMethodCache.Add(fieldType, methodInfo);
                        }
                        i.SetValue(t, methodInfo.Invoke(this, new object[] { arrayLengthAttribute.Length }));
                    }
                    else if (fieldType == typeof(TypeIndex))
                    {
                        i.SetValue(t, ReadTypeIndex());
                    }
                    else if (fieldType == typeof(TypeDefinitionIndex))
                    {
                        i.SetValue(t, ReadTypeDefinitionIndex());
                    }
                    else if (fieldType == typeof(GenericContainerIndex))
                    {
                        i.SetValue(t, ReadGenericContainerIndex());
                    }
                    else if (fieldType == typeof(ParameterIndex))
                    {
                        i.SetValue(t, ReadParameterIndex());
                    }
                    else if (fieldType == typeof(MethodIndex))
                    {
                        i.SetValue(t, ReadMethodIndex());
                    }
                    else if (fieldType == typeof(FieldIndex))
                    {
                        i.SetValue(t, ReadFieldIndex());
                    }
                    else if (fieldType == typeof(EventIndex))
                    {
                        i.SetValue(t, ReadEventIndex());
                    }
                    else if (fieldType == typeof(PropertyIndex))
                    {
                        i.SetValue(t, ReadPropertyIndex());
                    }
                    else if (fieldType == typeof(NestedTypeIndex))
                    {
                        i.SetValue(t, ReadNestedTypeIndex());
                    }
                    else if (fieldType == typeof(InterfacesIndex))
                    {
                        i.SetValue(t, ReadInterfacesIndex());
                    }
                    else if (fieldType == typeof(GenericParameterIndex))
                    {
                        i.SetValue(t, ReadGenericParameterIndex());
                    }
                    else if (fieldType == typeof(DefaultValueDataIndex))
                    {
                        i.SetValue(t, ReadDefaultValueDataIndex());
                    }
                    else if (fieldType == typeof(Il2CppSectionMetadata))
                    {
                        i.SetValue(t, ReadClass<Il2CppSectionMetadata>());
                    }
                    else
                    {
                        if (!genericMethodCache.TryGetValue(fieldType, out var methodInfo))
                        {
                            methodInfo = readClass.MakeGenericMethod(fieldType);
                            genericMethodCache.Add(fieldType, methodInfo);
                        }
                        i.SetValue(t, methodInfo.Invoke(this, null));
                    }
                }
                return t;
            }
        }

        public T[] ReadClassArray<T>(long count) where T : new()
        {
            var t = new T[count];
            for (var i = 0; i < count; i++)
            {
                t[i] = ReadClass<T>();
            }
            return t;
        }

        public T[] ReadClassArray<T>(ulong addr, ulong count) where T : new()
        {
            return ReadClassArray<T>(addr, (long)count);
        }

        public T[] ReadClassArray<T>(ulong addr, long count) where T : new()
        {
            Position = addr;
            return ReadClassArray<T>(count);
        }

        public string ReadStringToNull(ulong addr)
        {
            Position = addr;
            var bytes = new List<byte>();
            byte b;
            while ((b = ReadByte()) != 0)
                bytes.Add(b);
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public long ReadIntPtr()
        {
            return Is32Bit ? ReadInt32() : ReadInt64();
        }

        public virtual ulong ReadUIntPtr()
        {
            return Is32Bit ? ReadUInt32() : ReadUInt64();
        }

        public ulong PointerSize
        {
            get => Is32Bit ? 4ul : 8ul;
        }

        public BinaryReader Reader => reader;

        public BinaryWriter Writer => writer;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                reader.Dispose();
                writer.Dispose();
                stream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
