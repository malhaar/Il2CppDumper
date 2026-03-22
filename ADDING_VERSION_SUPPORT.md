# Adding Support for New IL2CPP Metadata Versions

This guide documents how to add support for new Unity IL2CPP metadata versions to Il2CppDumper.

## Background

Unity's IL2CPP compiler embeds a `global-metadata.dat` file alongside every IL2CPP build. This file contains all type definitions, method signatures, string literals, and other metadata needed at runtime. Each Unity release may increment the metadata version and change the binary format.

The version number is stored at byte offset 4 in the file, right after the magic bytes `0xFAB11BAF`.

## Where to Find Unity IL2CPP Source Headers

### Option 1: libil2cpp-archive (Recommended)

The **[js6pak/libil2cpp-archive](https://github.com/js6pak/libil2cpp-archive)** repository is a comprehensive archive of Unity's `libil2cpp` source across versions. It has **1,446+ tags** covering Unity releases from early versions through Unity 6000.x, updated regularly.

**Key files for version support:**
```
il2cpp-metadata.h                      # All metadata struct definitions
vm/
├── GlobalMetadata.cpp                 # How the runtime reads the header and sections
├── GlobalMetadataFileInternals.h      # Header struct internals
├── MetadataDeserialization.h          # Index sizing/compression logic
├── MetadataDeserialization.cpp        # Index deserialization implementation
├── MetadataCache.cpp/h               # Metadata caching layer
└── MetadataLoader.cpp/h              # Metadata file loading
```

**How to use for diffing between versions:**
```bash
# Clone the archive (or your fork of it)
git clone https://github.com/js6pak/libil2cpp-archive.git
cd libil2cpp-archive

# List available tags to find the Unity versions you care about
git tag | grep "6000" | sort -V

# Diff the key header files between two Unity versions
git diff <old-tag> <new-tag> -- il2cpp-metadata.h
git diff <old-tag> <new-tag> -- vm/GlobalMetadataFileInternals.h
git diff <old-tag> <new-tag> -- vm/MetadataDeserialization.h
git diff <old-tag> <new-tag> -- vm/GlobalMetadata.cpp
```

This is the fastest way to see exactly what struct fields, sections, or index types changed between Unity releases without needing a Unity installation.

### Option 2: Local Unity Installation

The same files exist in any Unity installation:

```
<Unity Install>/Editor/Data/il2cpp/libil2cpp/
├── il2cpp-metadata.h
├── vm/
│   ├── GlobalMetadata.cpp
│   ├── GlobalMetadataFileInternals.h
│   └── MetadataDeserialization.h
```

**Paths by platform:**

**macOS:**
```
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/il2cpp/libil2cpp/
```

**Windows:**
```
C:\Program Files\Unity\Hub\Editor\<version>\Editor\Data\il2cpp\libil2cpp\
```

**Linux:**
```
~/Unity/Hub/Editor/<version>/Editor/Data/il2cpp/libil2cpp/
```

### Getting a Sample Metadata File

1. Create a minimal Unity project
2. Set scripting backend to IL2CPP (Project Settings → Player → Scripting Backend)
3. Build for any platform
4. Find `global-metadata.dat` in the build output (typically in `<build>_Data/il2cpp_data/Metadata/`)

## Architecture Overview

### Version Gating with Attributes

Fields in metadata structs use `[Version(Min=X, Max=Y)]` attributes to declare which versions they exist in:

```csharp
[Version(Max = 35)]
public uint stringLiteralOffset;    // Only exists in versions ≤ 35

[Version(Min = 38)]
public Il2CppSectionMetadata stringLiterals;  // Only exists in versions ≥ 38
```

The `ReadClass<T>()` method in `BinaryStream.cs` checks these attributes at runtime and skips fields that don't match the current version.

### Variable-Width Index Types (v38+)

Starting in v38, Unity compresses index fields based on the number of items:

| Item Count  | Index Size |
|-------------|-----------|
| ≤ 255       | 1 byte    |
| ≤ 65,535    | 2 bytes   |
| > 65,535    | 4 bytes   |

This is handled by wrapper types (`TypeIndex`, `TypeDefinitionIndex`, `GenericContainerIndex`, `ParameterIndex`) with corresponding `ReadXxxIndex()` methods in `BinaryStream.cs`.

### Section Header Changes (v38+)

Before v38, each section used separate `offset` and `size` fields. From v38 onward, sections use `Il2CppSectionMetadata`:

```csharp
public class Il2CppSectionMetadata
{
    public uint offset;
    public uint size;
    public uint count;
}
```

## Step-by-Step: Adding a New Version

### Step 1: Diff the Unity Headers

Using the libil2cpp-archive repo:
```bash
cd libil2cpp-archive

# Find which tags correspond to the metadata version bump
# Check the version constant in the header:
git show <tag>:vm/GlobalMetadataFileInternals.h | grep "kGlobalMetadataVersion"

# Once you identify the two tags, diff the critical files:
git diff <old-tag> <new-tag> -- il2cpp-metadata.h
git diff <old-tag> <new-tag> -- vm/GlobalMetadataFileInternals.h
git diff <old-tag> <new-tag> -- vm/MetadataDeserialization.h
git diff <old-tag> <new-tag> -- vm/GlobalMetadata.cpp
```

Look for:

- New, removed, or reordered fields in structs
- New section types in the header
- Changes to index sizing logic
- New struct definitions

### Step 2: Update `MetadataClass.cs`

Add version attributes to mark where fields start/stop existing:

```csharp
// Mark the old field as ending at the previous version
[Version(Max = 39)]
public uint oldField;

// Add new field starting at the new version
[Version(Min = 40)]
public Il2CppSectionMetadata newSection;
```

If Unity added entirely new structs, define them in this file.

### Step 3: Update `Metadata.cs`

**Bump the version range:**
```csharp
// Change this line:
if (version < 16 || version > 39)
// To:
if (version < 16 || version > 40)
```

**Add version branches for reading data:**
```csharp
someData = Version < 40
    ? ReadMetadataClassArray<T>(header.oldOffset, header.oldSize)
    : ReadMetadataClassArray<T>(header.newSection.offset, (int)header.newSection.size);
```

**Update index size inference if the sizing logic changed.**

### Step 4: Update `BinaryStream.cs` (if new index types)

Add a new reader following the existing pattern:

```csharp
public NewIndex ReadNewIndex()
{
    if (Version < 40)
    {
        int value = ReadInt32();
        return new NewIndex(value);
    }
    else
    {
        switch (Metadata.newIndexSize)
        {
            case 1:
                uint value = ReadByte();
                if (value == Byte.MaxValue) return new NewIndex(-1);
                return new NewIndex((int)value);
            case 2:
                uint value = ReadUInt16();
                if (value == UInt16.MaxValue) return new NewIndex(-1);
                return new NewIndex((int)value);
            case 4:
            default:
                uint value = ReadUInt32();
                if (value == UInt32.MaxValue) return new NewIndex(-1);
                return new NewIndex((int)value);
        }
    }
}
```

Add the type handling to `ReadClass<T>()`:
```csharp
else if (fieldType == typeof(NewIndex))
{
    i.SetValue(t, ReadNewIndex());
}
```

### Step 5: Update Output Files (if needed)

`Il2CppDecompiler.cs` and `StructGenerator.cs` only need changes if:
- Custom attribute handling changed
- Method pointer resolution changed
- New kinds of metadata are being dumped

## Validation

1. **Build:** `dotnet build`
2. **Run:** Test against your sample `global-metadata.dat` + the matching IL2CPP binary
3. **Verify output:** Check that `dump.cs` contains sensible class/method names (not garbage or empty)
4. **Cross-check:** If possible, compare output against a dump from a game that also ships with an older Unity version to ensure existing versions still work

## Staying Updated

- **Upstream:** Watch [Perfare/Il2CppDumper](https://github.com/nicknisi/Il2CppDumper) and active forks (e.g., roytu) for PRs
- **Unity releases:** Check release notes for IL2CPP-related changes
- **Quick test:** Run your fork against new Unity builds — a `NotSupportedException` with an unknown version number means it's time to add support

## Version History Reference

| Metadata Version | Key Changes |
|-----------------|-------------|
| v35 | Variable-width indices introduced (but still 4 bytes). Header section format begins transition. |
| v38 | Full `Il2CppSectionMetadata` header format. Index sizes become truly variable (1/2/4 bytes). TypeIndex, TypeDefinitionIndex, GenericContainerIndex compressed. String literal reading changed. Interface indices became `InterfaceOffsetPair`. |
| v39 | ParameterIndex also becomes variable-width. |
