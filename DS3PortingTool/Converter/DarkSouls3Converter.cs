using DS3PortingTool.Util;
using SoulsFormats;

namespace DS3PortingTool.Converter;

public class DarkSouls3Converter : Converter
{
    public override void DoConversion(Options op)
    {
        BND4 inputBinder = (BND4)ReadSourceBinder(op.CurrentContentSourceFile, op);
        IBinder outputBinder = op.InputGame.BinderVersion == Game.BinderVersionType.BND3 ? new BND3() : new BND4();
        
        PortAnimations(inputBinder, outputBinder, op);
    }
    
    /// <summary>
    /// Performs the steps necessary to convert a DS3 binder into a new DS3 binder.
    /// </summary>
    protected override void ConvertTo_ER(Options op)
    {
        BND4 sourceBnd = (BND4)op.CurrentTextureSourceFile;
        
        BND4 newBnd = new();
        if (op.CurrentTextureSourceFileName.Contains("anibnd"))
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

            if (!op.PortTaeOnly)
            {
                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                newBnd.Write($"{op.Cwd}\\c{op.PortedId}.anibnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
            }
        }
        else if (op.CurrentTextureSourceFileName.Contains("chrbnd"))
        {
            /*ConvertCharacterHkx(sourceBnd, newBnd, op);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}.hkx")))
            {
                sourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}.hkxpwv");
            }
		
            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}_c.hkx")))
            {
                sourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}_c.clm2",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}_c.clm2");
            }*/
            
            BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedId}.chrbnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
        else if (op.CurrentTextureSourceFileName.Contains("objbnd"))
        {
            BinderFile file1 = sourceBnd.Files.First(x => FLVER2.Is(x.Bytes));
            FLVER2 flver1 = FLVER2.Read(file1.Bytes);
            BinderFile file2 = (op.ContentSourceFiles[Array.IndexOf(op.ContentSourceFiles, op.CurrentTextureSourceFile) + 1] as BND4)
                .Files.First(x => FLVER2.Is(x.Bytes));
            FLVER2 flver2 = FLVER2.Read(file2.Bytes);
            
            
            BinderFile? file = sourceBnd.Files.Find(x => x.Name.EndsWith(".anibnd"));
            if (file != null)
            {
                file = BND4.Read(file.Bytes).Files.Find(x => x.Name.Contains(".tae"));
                if (file != null)
                {
                    ConvertObjectTae(newBnd, file, op);
                }
            }
        }
    }

    protected override void ConvertCharacterHkx(IBinder sourceBnd, BND4 newBnd, Options op)
    {
        newBnd.Files = sourceBnd.Files
            .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();
            
            if (name.Contains($"c{op.SourceId}.hkx") || name.Contains($"c{op.SourceId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(op.SourceId, op.PortedId)}";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
            }
        }
    }

    protected override void ConvertObjectHkx(IBinder sourceBnd, BND4 newBnd, Options op, bool isInnerAnibnd)
    {
        throw new NotImplementedException();
    }

    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data)
    {
        return ev;
    }

    /// <summary>
    /// Converts a ds3 FLVER file into a new DS3 FLVER file.
    /// </summary>
    private new void ConvertFlver(BND4 newBnd, BinderFile flverFile, Options op)
    {
        FLVER2 newFlver = FLVER2.Read(flverFile.Bytes);

        if (op.ContentSourceFileNames.Any(x => x.Contains(".texbnd")))
        {
            foreach (FLVER2.Material mat in newFlver.Materials)
            {
                foreach (FLVER2.Texture tex in mat.Textures)
                {
                    tex.Path = tex.Path.Replace($"c{op.SourceId}", $"c{op.PortedId}");
                }
            }
        }
        
        flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\c{op.PortedId}.flver",
            newFlver.Write());
        newBnd.Files.Add(flverFile);
    }
}