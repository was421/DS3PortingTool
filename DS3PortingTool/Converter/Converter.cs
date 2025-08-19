using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;
using DS3PortingTool.Util;
using FlverFixer;
using FLVERMaterialHelper;
using FLVERMaterialHelper.MatShaderInfoBank;
using ImageMagick;
using SoulsFormats;

namespace DS3PortingTool.Converter;

public abstract class Converter
{
    public List<HashSet<string>> UsedTextureGroups = new();
    
    public List<TPF.Texture> Textures = new();
    public List<TPF.Texture> LODTextures = new();
    
    /// <summary>
    /// Performs the steps necessary to convert a foreign binder into a DS3 compatible binder.
    /// </summary>
    public virtual void DoConversion(Options op)
    {
        IBinder sourceBnd = (IBinder)op.CurrentTextureSourceFile;
        
        BND4 newBnd = new();
        if (op.CurrentTextureSourceFileName.Contains("anibnd") && op.SourceBndsType == Options.AssetType.Character)
        {
            if (!op.PortTaeOnly)
            {
                ConvertCharacterHkx(sourceBnd, newBnd, op);
            }
            
            BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                ConvertCharacterTae(sourceBnd, newBnd, file, op);
            }

            if (op.PortTaeOnly) return;
            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedId}.anibnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
        else if (op.CurrentTextureSourceFileName.Contains("chrbnd") && op.SourceBndsType == Options.AssetType.Character)
        {
            if (!op.PortFlverOnly)
            {
                ConvertCharacterHkx(sourceBnd, newBnd, op);

                if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}.hkx")))
                {
                    sourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}.hkxpwv",
                        @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}.hkxpwv");
                }

                if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}_c.hkx")))
                {
                    sourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}_c.clm2",
                        @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}_c.clm2");
                }
            }

            BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }

            if (op.PortFlverOnly) return;
            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedId}.chrbnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
        else if (op.CurrentTextureSourceFileName.Contains("objbnd") && op.SourceBndsType == Options.AssetType.Object)
        {
            if (!op.PortTaeOnly && !op.PortFlverOnly)
            {
                ConvertObjectHkx(sourceBnd, newBnd, op, false);

                if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"o{op.PortedId}_c.hkx")))
                {
                    sourceBnd.TransferBinderFile(newBnd, $"o{op.SourceId}_c.clm2",
                        @"N:\FDP\data\INTERROOT_win64\obj\" +
                        $"o{op.PortedId[..2]}\\o{op.PortedId}\\o{op.PortedId}_c.clm2");
                }
            }

            BinderFile? file = sourceBnd.Files.Find(x => x.Name.EndsWith(".anibnd"));
            if (file != null && !op.PortFlverOnly)
            {
                BND4 oldAnibnd = BND4.Read(file.Bytes);
                BND4 newAnibnd = new();

                if (!op.PortTaeOnly)
                {
                    ConvertObjectHkx(sourceBnd, newAnibnd, op, true);
                }

                file = oldAnibnd.Files.Find(x => x.Name.Contains(".tae"));
                if (file != null)
                {
                    ConvertObjectTae(newAnibnd, file, op);
                }

                if (!op.PortTaeOnly)
                {
                    newAnibnd.Files = newAnibnd.Files.OrderBy(x => x.ID).ToList();
                    newBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 400,
                        $"N:\\FDP\\data\\INTERROOT_win64\\obj\\" +
                        $"o{op.PortedId[..2]}\\o{op.PortedId}\\o{op.PortedId}.anibnd",
                        newAnibnd.Write()));
                }
            }

            if (op.PortTaeOnly) return;
            foreach (BinderFile flver in sourceBnd.Files
                         .Where(x => FLVER2.Is(x.Bytes) && !x.Name
                             .EndsWith("_S.flver", StringComparison.OrdinalIgnoreCase)))
            {
                ConvertFlver(newBnd, flver, op);
            }
            
            if (op.PortFlverOnly) return;
            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\o{op.PortedId}.objbnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
    }
    /// <summary>
    /// Converts a foreign character HKX file into a DS3 compatible HKX file.
    /// </summary>
    protected abstract void ConvertCharacterHkx(IBinder sourceBnd, BND4 newBnd, Options op);
    /// <summary>
    /// Converts a foreign object HKX file into a DS3 compatible HKX file.
    /// </summary>
    protected abstract void ConvertObjectHkx(IBinder sourceBnd, BND4 newBnd, Options op, bool isInnerAnibnd);
    /// <summary>
    /// Converts a foreign character TAE file into a DS3 compatible TAE file.
    /// </summary>
    protected virtual void ConvertCharacterTae(IBinder sourceBnd, BND4 newBnd, BinderFile taeFile, Options op)
    {
        TAE oldTae = TAE.Read(taeFile.Bytes);
        TAE newTae = new()
        {
            Format = TAE.TAEFormat.DS3,
            BigEndian = false,
            ID = 200000 + int.Parse(op.PortedId),
            Flags = new byte[] { 1, 0, 1, 2, 2, 1, 1, 1 },
            SkeletonName = "skeleton.hkt",
            SibName = $"c{op.PortedId}.sib",
            Animations = new List<TAE.Animation>(),
            EventBank = 21
        };

        XmlData data = new(op);
        
        data.ExcludedAnimations.AddRange(oldTae.Animations
            .Where(x => x.GetOffset() > 0 && data.ExcludedAnimations.Contains(x.GetNoOffsetId()))
            .Select(x => Convert.ToInt32(x.ID)));

        data.ExcludedAnimations.AddRange(oldTae.Animations.Where(x => 
                x.MiniHeader is TAE.Animation.AnimMiniHeader.Standard { ImportsHKX: true } standardHeader && 
                sourceBnd.Files.All(y => y.Name != "a00" + standardHeader.ImportHKXSourceAnimID.ToString("D3").GetOffset() +
                    "_" + standardHeader.ImportHKXSourceAnimID.ToString("D9")[3..] + ".hkx"))
            .Select(x => Convert.ToInt32(x.ID)));

        data.ExcludedAnimations.AddRange(oldTae.Animations.Where(x =>
                x.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim otherHeader &&
                data.ExcludedAnimations.Contains(otherHeader.ImportFromAnimID))
            .Select(x => Convert.ToInt32(x.ID)));
		
        data.ExcludedAnimations.AddRange(oldTae.GetExcludedOffsetAnimations(op));

        newTae.Animations = oldTae.Animations
            .Where(x => !data.ExcludedAnimations.Contains(Convert.ToInt32(x.ID))).ToList();

        foreach (TAE.Animation? anim in newTae.Animations)
        {
            anim.RemapImportAnimationId(data);
			
            if (data.AnimationRemapping.ContainsKey(anim.GetNoOffsetId()))
            {
                data.AnimationRemapping.TryGetValue(anim.GetNoOffsetId(), out int newAnimId);
                anim.SetAnimationProperties(newAnimId, anim.GetNoOffsetId(), anim.GetOffset(), op);
            }
            else
            {
                anim.SetAnimationProperties(anim.GetNoOffsetId(), anim.GetNoOffsetId(), anim.GetOffset(), op);
            }
			
            anim.Events = anim.Events.Where(ev => 
                    (!data.ExcludedEvents.Contains(ev.Type) || ev.IsAllowedSpEffect(newTae.BigEndian, data)) && 
                    !data.ExcludedJumpTables.Contains(ev.GetJumpTableId(newTae.BigEndian)) && 
                    !data.ExcludedRumbleCams.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
                .Select(ev => EditEvent(ev, newTae.BigEndian, op, data)).ToList();
            
        }
		
        if (op.ExcludedAnimOffsets.Any())
        {
            newTae.ShiftAnimationOffsets(op);
        }

        oldTae.Animations = oldTae.Animations.OrderBy(x => x.ID).ToList();
        newTae.Animations = newTae.Animations.OrderBy(x => x.ID).ToList();
        
        taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\tae\\c{op.PortedId}.tae",
            newTae.Write());
		
        if (op.PortTaeOnly)
        {
            File.WriteAllBytes($"{op.Cwd}\\c{op.PortedId}.tae", taeFile.Bytes);
        }
        else
        {
            newBnd.Files.Add(taeFile);
        }
    }

    /// <summary>
    /// Converts a foreign object TAE file into a DS3 compatible TAE file.
    /// </summary>
    protected virtual void ConvertObjectTae(BND4 newBnd, BinderFile taeFile, Options op)
    {
        TAE oldTae = TAE.Read(taeFile.Bytes);
        TAE newTae = new()
        {
            Format = TAE.TAEFormat.DS3,
            BigEndian = false,
            ID = 200000 + int.Parse(op.PortedId),
            Flags = new byte[] { 1, 0, 1, 2, 2, 1, 1, 1 },
            SkeletonName = "skeleton.hkt",
            SibName = $"c{op.PortedId}.sib",
            Animations = oldTae.Animations,
            EventBank = 18
        };
        
        XmlData data = new(op);

        foreach (TAE.Animation? anim in newTae.Animations)
        {
            anim.SetAnimationProperties(anim.GetNoOffsetId(), anim.GetNoOffsetId(), anim.GetOffset(), op);
			
            anim.Events = anim.Events.Where(ev => 
                    (!data.ExcludedEvents.Contains(ev.Type) || ev.IsAllowedSpEffect(newTae.BigEndian, data)) && 
                    !data.ExcludedJumpTables.Contains(ev.GetJumpTableId(newTae.BigEndian)) && 
                    !data.ExcludedRumbleCams.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
                .Select(ev => EditEvent(ev, newTae.BigEndian, op, data)).ToList();
            
        }
        
        newTae.Animations = newTae.Animations.OrderBy(x => x.ID).ToList();
        
        taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
            $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId[..2]}\\o{op.PortedId}\\tae\\o{op.PortedId}.tae",
            newTae.Write());
		
        if (op.PortTaeOnly)
        {
            File.WriteAllBytes($"{op.Cwd}\\o{op.PortedId}.tae", taeFile.Bytes);
        }
        else
        {
            newBnd.Files.Add(taeFile);
        }
    }

    /// <summary>
    /// Edits parameters of the event so that it will match with its DS3 event equivalent.
    /// </summary>
    protected abstract TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data);
    /// <summary>
    /// Converts a foreign FLVER file into a DS3 compatible FLVER file.
    /// </summary>
    protected void ConvertFlver(BND4 newBnd, BinderFile flverFile, Options op)
    {
        XmlData data = new(op);

        FLVER2 oldFlver = FLVER2.Read(flverFile.Bytes);
        FLVER2 newFlver = CreateDs3Flver(oldFlver, data, op);

        //List<FLVER2.Material> distinctMaterials = newFlver.Materials.DistinctBy(x => x.MTD).ToList();
        /*foreach (FLVER2.Material distinctMat in distinctMaterials)
        {
            FLVER2.GXList gxList = new FLVER2.GXList();
            gxList.AddRange(data.MaterialInfoBank
                .GetDefaultGXItemsForMTD(Path.GetFileName(distinctMat.MTD).ToLower()));

            if (newFlver.IsNewGxList(gxList))
            {
                newFlver.GXLists.Add(gxList);
            }
        }*/

        foreach (FLVER2.Mesh mesh in newFlver.Meshes)
        {
            FLVER2.Material mat = newFlver.Materials[mesh.MaterialIndex];

            HashSet<string> usedTextures = new HashSet<string>();
            
            // Get used textures
            foreach (FLVER2.Texture tex in mat.Textures)
            {
                if (tex.Path.Length == 0) continue;
                usedTextures.Add(Path.GetFileNameWithoutExtension(tex.Path));
            }
            
            UsedTextureGroups.Add(usedTextures);
            
            MatShaderInfoBank.MaterialInfo matInfo = data.MaterialInfoBank.MaterialInformation
                .First(x => x.MatName == Path.GetFileName(mat.MTD.ToLower()));
            MatShaderInfoBank.ShaderInfo shaderInfo = data.MaterialInfoBank.ShaderInformation
                .First(x => x.SpxName == matInfo.SpxName);
            
            EXParam exParam = EXParam.GenerateForMaterial_DS3(mat, data.MaterialInfoBank);
            if (exParam.Count > 0)
            {
                FLVER2.GXList newGXList = exParam.ExportToGXList_DS3();
                FLVER2.GXList? identicalGXList = newFlver.GXLists.FirstOrDefault(x => GXListExtensions.Equals(x,newGXList));
                if (identicalGXList != null)
                {
                    mat.GXIndex = newFlver.GXLists.IndexOf(identicalGXList);
                }
                else
                {
                    newFlver.GXLists.Add(newGXList);
                    mat.GXIndex = newFlver.GXLists.Count - 1;
                }
            }
            
            
            FLVER2.BufferLayout newLayout = new FLVER2.BufferLayout();
            int uvCount = 0;
            foreach (MatShaderInfoBank.ShaderInfo.LayoutMember member in shaderInfo.VertexBufferLayout)
            {
                FLVER.LayoutSemantic semantic;
                switch (member.Semantic)
                {
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.POSITION:
                        semantic = FLVER.LayoutSemantic.Position;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.NORMAL:
                        semantic = FLVER.LayoutSemantic.Normal;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.TANGENT:
                        semantic = FLVER.LayoutSemantic.Tangent;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.BINORMAL:
                        semantic = FLVER.LayoutSemantic.Bitangent;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.BLENDINDICES:
                        semantic = FLVER.LayoutSemantic.BoneIndices;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.BLENDWEIGHT:
                        semantic = FLVER.LayoutSemantic.BoneWeights;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.COLOR:
                        semantic = FLVER.LayoutSemantic.VertexColor;
                        break;
                    case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.TEXCOORD:
                        semantic = FLVER.LayoutSemantic.UV;
                        break;
                    default:
                        continue;
                }
                FLVER.LayoutType type;
                switch (member.Type)
                {
                    case "float2":
                        type = FLVER.LayoutType.Float2;
                        break;
                    case "float3":
                        type = FLVER.LayoutType.Float3;
                        break;
                    case "float4":
                        type = FLVER.LayoutType.UByte4Norm;
                        break;
                    case "int2":
                        type = FLVER.LayoutType.Short2;
                        break;
                    case "int4":
                        type = FLVER.LayoutType.Short4;
                        break;
                    case "uint":
                        type = FLVER.LayoutType.UByte4;
                        break;
                    case "uint4":
                        type = FLVER.LayoutType.UByte4;
                        break;
                    default:
                        continue;
                }

                if (semantic == FLVER.LayoutSemantic.UV)
                {
                    if (type == FLVER.LayoutType.Short2 || type == FLVER.LayoutType.Float2)
                    {
                        uvCount++;
                    }
                    else
                    {
                        uvCount += 2;
                    }
                }
                
                newLayout.Add(new FLVER.LayoutMember(type, semantic, member.Index, 0, 0));
            }

            foreach (FLVER.Vertex v in mesh.Vertices)
            {
                int currentUVCount = v.UVs.Count;
                for (int i = 0; i < uvCount - currentUVCount; i++)
                {
                    v.UVs.Add(new Vector3(0));
                }
            }

            if (newFlver.BufferLayouts.All(x => !BufferLayoutExtensions.Equals(x, newLayout)))
            {
                newFlver.BufferLayouts.Add(newLayout);
            }
                
        }
        
        

        if (op.SourceBndsType == Options.AssetType.Character)
        {
            flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
                $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\c{op.PortedId}.flver",
                newFlver.Write());
        }
        else if (op.SourceBndsType == Options.AssetType.Object)
        {
            if (flverFile.Name.EndsWith("_1.flver", StringComparison.OrdinalIgnoreCase))
            {
                flverFile = new BinderFile(Binder.FileFlags.Flag1, 201,
                    $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId.Substring(0, 2)}\\o{op.PortedId}\\o{op.PortedId}_1.flver",
                    newFlver.Write());
            }
            else
            {
                flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
                    $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId.Substring(0, 2)}\\o{op.PortedId}\\o{op.PortedId}.flver",
                    newFlver.Write());
            }
        }
        else if (op.SourceBndsType == Options.AssetType.MapPiece)
        {
            string map = $"m{op.PortedId[..2]}_{op.PortedId[2..4]}_{op.PortedId[4..6]}_{op.PortedId[6..8]}";
            string model = $"{map}_{op.PortedId[8..]}";
            flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
                $"N:\\FDP\\data\\INTERROOT_win64\\map\\{map}\\{model}\\Model\\{model}.flver",
                newFlver.Write());
        }
        
        if (op.PortFlverOnly)
        {
            File.WriteAllBytes($"{op.Cwd}\\{Path.GetFileName(flverFile.Name)}", flverFile.Bytes);
        }
        else
        {
            newBnd.Files.Add(flverFile);
        }
    }

    /// <summary>
    /// Creates a new DS3 FLVER using data from a foreign FLVER.
    /// </summary>
    protected virtual FLVER2 CreateDs3Flver(FLVER2 sourceFlver, XmlData data, Options op)
    {
        FLVER2 newFlver = new FLVER2
        {
            Header = new FLVER2.FLVERHeader
            {
                BoundingBoxMin = sourceFlver.Header.BoundingBoxMin,
                BoundingBoxMax = sourceFlver.Header.BoundingBoxMax,
                Unicode = sourceFlver.Header.Unicode,
                Unk4A = sourceFlver.Header.Unk4A,
                Unk4C = sourceFlver.Header.Unk4C,
                Unk5C = sourceFlver.Header.Unk5C,
                Unk5D = sourceFlver.Header.Unk5D,
                Unk68 = sourceFlver.Header.Unk68
            },
            Dummies = sourceFlver.Dummies,
            /*Materials = sourceFlver.Materials.Select(x => 
                op.UseBestFitMaterials ? 
                    x.ToDs3Material(sourceFlver, data.MaterialInfoBank, data.MatBins, op) :
                    x.ToDummyDs3Material(data.MaterialInfoBank, op)
                ).ToList(),*/
            Nodes = sourceFlver.Nodes.Select(x =>
            {
                // Flags should only be 0 or 1 in DS3.
                if (x.Flags is not 0 or FLVER.Node.NodeFlags.Disabled)
                {
                    x.Flags = 0;
                }

                return x;
            }).ToList(),
            Meshes = sourceFlver.Meshes
        };

        foreach (FLVER2.Material sourceMaterial in sourceFlver.Materials)
        {
            TextureInfo textureInfo = new(sourceMaterial, data.MatBins);
            
            FLVER2.Material newMaterial = op.UseBestFitMaterials
                ? sourceMaterial.ToDs3Material(textureInfo, sourceFlver, data.MaterialInfoBank, data.MatBins, op)
                : sourceMaterial.ToDummyDs3Material(textureInfo, data.MaterialInfoBank, op);
            
            newFlver.Materials.Add(newMaterial);
        }

        if (op.SourceBndsType == Options.AssetType.Object || op.SourceBndsType == Options.AssetType.MapPiece)
        {
            BoundingBoxSolver.FixAllBoundingBoxes(newFlver);
        }
        
        return newFlver;
    }
    
    /// <summary>
    ///	Downgrades a newer HKX file to Havok 2014.
    /// </summary>
    protected virtual bool PortHavok(BinderFile hkxFile, string toolsDirectory)
    {
        string hkxName = Path.GetFileName(hkxFile.Name);
        File.WriteAllBytes($"{toolsDirectory}\\{hkxName}", hkxFile.Bytes);
        string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
        
        // FileConvert
        bool result = RunProcess(toolsDirectory, "fileConvert.exe", 
            $"-x {toolsDirectory}\\{hkxName} {toolsDirectory}\\{xmlName}");
        File.Delete($"{toolsDirectory}\\{hkxName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
		
        // DS3HavokConverter
        result = RunProcess(toolsDirectory,"DS3HavokConverter.exe", 
            $"{toolsDirectory}\\{xmlName}");
        if (File.Exists($"{toolsDirectory}\\{xmlName}.bak"))
        {
            File.Delete($"{toolsDirectory}\\{xmlName}.bak");
        }
		
        if (result == false)
        {
            File.Delete($"{toolsDirectory}\\{xmlName}");
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
		
        // Repack xml file
        result = RunProcess(toolsDirectory,"hkxpackds3.exe",
            $"{toolsDirectory}\\{xmlName}"); 
        File.Delete($"{toolsDirectory}\\{xmlName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
		
        hkxFile.Bytes = File.ReadAllBytes($"{toolsDirectory}\\{hkxName}");
        File.Delete($"{toolsDirectory}\\{hkxName}");
        Console.WriteLine($"Downgraded {hkxName}");
        return true;
    }
    
    /// <summary>
    /// Downgrades a newer HKX file to Havok 2014 using an HKX Compendium.
    /// Use when porting animations from Sekiro and Elden Ring.
    /// </summary>
    protected virtual bool PortHavok(BinderFile hkxFile, string toolsDirectory, BinderFile compendium)
    {
        // Copy compendium
        string compendiumPath = $"{toolsDirectory}\\" + Path.GetFileName(compendium.Name);
        File.WriteAllBytes(compendiumPath, compendium.Bytes);
		
        string hkxName = Path.GetFileName(hkxFile.Name);
        File.WriteAllBytes($"{toolsDirectory}\\{hkxName}", hkxFile.Bytes);
        string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
		
        // FileConvert
        bool result = RunProcess(toolsDirectory,"fileConvert.exe", 
            $"-x --compendium {compendiumPath} {toolsDirectory}\\{hkxName} {toolsDirectory}\\{xmlName}");
        File.Delete($"{toolsDirectory}\\{hkxName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
		
        // DS3HavokConverter
        result = RunProcess(toolsDirectory,"DS3HavokConverter.exe", 
            $"{toolsDirectory}\\{xmlName}");
        if (File.Exists($"{toolsDirectory}\\{xmlName}.bak"))
        {
            File.Delete($"{toolsDirectory}\\{xmlName}.bak");
        }
		
        if (result == false)
        { 
            File.Delete($"{toolsDirectory}\\{xmlName}");
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
		
        // Repack xml file
        result = RunProcess(toolsDirectory,"hkxpack-souls.exe",
            $"{toolsDirectory}\\{xmlName}"); 
        File.Delete($"{toolsDirectory}\\{xmlName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }

        hkxFile.Bytes = File.ReadAllBytes($"{toolsDirectory}\\{hkxName}");
        File.Delete($"{toolsDirectory}\\{hkxName}");
        File.Delete(compendiumPath);
        Console.WriteLine($"Downgraded {hkxName}");
        return true;
    }
    
    /// <summary>
    ///	Run an external tool with the given arguments.
    /// </summary>
    public static bool RunProcess(string searchDir, string applicationName, string args)
    {
        string[] results = Directory.GetFiles(searchDir, $"{applicationName}",
            SearchOption.AllDirectories);
		
        if (!results.Any())
        {
            throw new FileNotFoundException($"Could not find the application \"{applicationName}\" in HavokDowngrade.");
        }

        string toolPath = results.First();
		
        Process tool = new();
        tool.StartInfo.FileName = toolPath;
        tool.StartInfo.Arguments = args;
        tool.StartInfo.WorkingDirectory = Path.GetDirectoryName(toolPath);
        tool.StartInfo.RedirectStandardOutput = true;
        tool.StartInfo.RedirectStandardError = true;
        tool.StartInfo.RedirectStandardInput = true;
        tool.Start();
        while (tool.HasExited == false)
        {
            tool.StandardInput.Close();
        }

        if (tool.StandardError.ReadToEnd().Length > 0)
        {
            return false;
        }

        return true;
    }

    protected virtual void WritePBRCorrectedDDS(HashSet<string> texGroup, Options op)
    {
        List<string> prunedTexGroup = texGroup.Where(x => Textures.Any(y => y.Name == x)).ToList();
        
        string[] albedoTex = prunedTexGroup.Where(x => x.EndsWith("a", StringComparison.OrdinalIgnoreCase)).ToArray();
        string[] metallicTex = prunedTexGroup.Where(x => x.EndsWith("r", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (albedoTex.Length == metallicTex.Length)
        {
            for (int i = 0; i < albedoTex.Length; i++)
            {
                TPF.Texture albedoTpfTex = Textures.First(x => x.Name == albedoTex[i]);
                TPF.Texture metallicTpfTex = Textures.First(x => x.Name == metallicTex[i]);
                byte[] albedoBytes = MagickImageFromTPFBytes(albedoTpfTex.Bytes, op).ToByteArray();
                byte[] metallicBytes = MagickImageFromTPFBytes(metallicTpfTex.Bytes, op).ToByteArray();

                albedoTpfTex.Bytes = ConvertAlbedoToSpecularPBR(new MagickImage(albedoBytes), new MagickImage(metallicBytes), op);
                WriteDDSBytes(albedoTex[i], albedoTpfTex.Bytes, op);

                metallicTpfTex.Bytes = ConvertMetallicToSpecularPBR(new MagickImage(albedoBytes), new MagickImage(metallicBytes), op);
                WriteDDSBytes(metallicTex[i], metallicTpfTex.Bytes, op);
            }
        }
        else
        {
            Console.WriteLine();
        }
        
        Console.WriteLine();
    }

    public byte[] ConvertAlbedoToSpecularPBR(MagickImage albedoImage, MagickImage metallicImage, Options op)
    {
        MagickImage albedoFillLayer = new(MagickColors.Black, albedoImage.Width, albedoImage.Height);
        albedoFillLayer.Format = MagickFormat.Dds;
        metallicImage.Resize(albedoImage.Width, albedoImage.Height);
        albedoFillLayer.Composite(metallicImage);
        IPixelCollection<byte> albedoPixels = albedoImage.GetPixels();
        IPixelCollection<byte> fillLayerPixels = albedoFillLayer.GetPixels();
        for (int u = 0; u < albedoImage.Height; u++)
        {
            for (int v = 0; v < albedoImage.Width; v++)
            {
                IPixel<byte> albedoPixel = albedoPixels[u, v];
                IPixel<byte> fillLayerPixel = fillLayerPixels[u, v];

                for (uint w = 0; w < 3; w++)
                {
                    float multpart1 = fillLayerPixel.GetChannel(w);
                    float multiplier = multpart1 / 255;
                    albedoPixel.SetChannel(w, (byte)(albedoPixel.GetChannel(w) * multiplier));
                }
            }
        }

        return ReadWriteDDSBytes(albedoImage.ToByteArray(), op);
    }

    public byte[] ConvertMetallicToSpecularPBR(MagickImage albedoImage, MagickImage metallicImage, Options op)
    {
        albedoImage.Resize(metallicImage.Width, metallicImage.Height);
                
        MagickImage specularFillLayer = new(new MagickColor("#383838"), metallicImage.Width, metallicImage.Height);
        specularFillLayer.Format = MagickFormat.Dds;
        specularFillLayer.Composite(metallicImage);
        specularFillLayer.Negate(Channels.RGB);
                
        IPixelCollection<byte> albedoPixels = albedoImage.GetPixels();
        IPixelCollection<byte> fillLayerPixels = specularFillLayer.GetPixels();
        for (int u = 0; u < albedoImage.Height; u++)
        {
            for (int v = 0; v < albedoImage.Width; v++)
            {
                IPixel<byte> albedoPixel = albedoPixels[u, v];
                IPixel<byte> fillLayerPixel = fillLayerPixels[u, v];

                for (uint w = 0; w < 3; w++)
                {
                    float multpart1 = fillLayerPixel.GetChannel(w);
                    float multiplier = multpart1 / 255;
                    albedoPixel.SetChannel(w, (byte)(albedoPixel.GetChannel(w) * multiplier));
                }
            }
        }
        
        return ReadWriteDDSBytes(albedoImage.ToByteArray(), op);
    }

    public MagickImage MagickImageFromTPFBytes(byte[] bytes, Options op)
    {
        byte[] fixedBytes = ReadWriteDDSBytes(bytes, op); // re-read DDS to make compatible with image magick
        MagickImage image = new MagickImage(fixedBytes);
        return image;
    }

    public byte[] ReadWriteDDSBytes(byte[] bytes, Options op)
    {
        File.WriteAllBytes(op.Cwd + "temp.dds", bytes);
        ProcessStartInfo psi = new()
        {
            FileName = "texconv.exe",
        };
        psi.Arguments = "-f BC1_UNORM -srgb -y temp.dds";
        Process process = new Process()
        {
            StartInfo = psi
        };
        process.Start();
        process.WaitForExit();
        
        bytes = File.ReadAllBytes(op.Cwd + "temp.dds");
        File.Delete(op.Cwd + "temp.dds");
        return bytes;
    }

    public void WriteDDSBytes(string outputName, byte[] bytes, Options op)
    {
        string ddsPath = op.Cwd + outputName + ".dds";
        File.WriteAllBytes(ddsPath, bytes);
        ProcessStartInfo psi = new()
        {
            FileName = "texconv.exe",
        };
        psi.Arguments = $"-f BC1_UNORM -srgb -y {ddsPath}";
        Process process = new Process()
        {
            StartInfo = psi
        };
        process.Start();
        process.WaitForExit();
    }
}