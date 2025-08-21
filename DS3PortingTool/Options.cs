using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using SoulsFormats;
using ArgumentException = System.ArgumentException;

namespace DS3PortingTool;

public class Options
{
    public enum AssetType
    {
        Character,
        Object,
        MapPiece
    }
    
    /// <summary>
    /// The current-working directory.
    /// </summary>
    public string Cwd { get; }
    /// <summary>
    /// Name(s) of the source dcx file(s) without the path.
    /// </summary>
    public string[] ContentSourceFileNames { get; }
    /// <summary>
    /// The binder(s) where data being ported is sourced from.
    /// </summary>
    public ISoulsFile[] ContentSourceFiles { get; }
    /// <summary>
    /// Name(s) of the source dcx file(s) without the path.
    /// </summary>
    public string[] TextureSourceFileNames { get; }
    /// <summary>
    /// The binder(s) where data being ported is sourced from.
    /// </summary>
    public ISoulsFile[] TextureSourceFiles { get; }
    /// <summary>
    /// Name of the source dcx file without the path currently being ported.
    /// </summary>
    public string CurrentContentSourceFileName { get; set; }
    /// <summary>
    /// The binder currently being ported.
    /// </summary>
    public ISoulsFile CurrentContentSourceFile { get; set; }
    /// <summary>
    /// Name of the source dcx file without the path currently being ported.
    /// </summary>
    public string CurrentTextureSourceFileName { get; set; }
    /// <summary>
    /// The binder currently being ported.
    /// </summary>
    public ISoulsFile CurrentTextureSourceFile { get; set; }
    /// <summary>
    /// What type of asset the source bnds are for.
    /// </summary>
    public AssetType SourceBndsType { get; }
    /// <summary>
    /// The game that the source binder comes from.
    /// </summary>
    public Game InputGame { get; }
    /// <summary>
    /// define what game the output binder will be for.
    /// </summary>
    public Game OutputGame { get; }
    /// /// <summary>
    /// The id of the source binder.
    /// </summary>
    public string SourceId { get; }
    /// <summary>
    /// The id of the ported binder.
    /// </summary>
    public string PortedId { get; }
    /// <summary>
    /// The id that sound events will use.
    /// </summary>
    public string SoundId { get; }
    /// <summary>
    /// The id that lock cam param events will use.
    /// </summary>
    public string LockCamParamId { get; }
    /// <summary>
    /// The length of the source and ported id.
    /// </summary>
    public int IdLength { get; }
    /// <summary>
    /// Flag setting which if true means only the tae will be ported when porting an anibnd.
    /// </summary>
    public bool PortTaeOnly { get; }
    /// <summary>
    /// Flag setting which if true means only the flver will be ported if there is one.
    /// </summary>
    public bool PortFlverOnly { get; }
    /// <summary>
    /// Flag setting which if true means that sound ids will be changed to match new character id.
    /// </summary>
    public bool ChangeSoundIds { get; }
    /// <summary>
    /// Flag setting which if true means that lock cam param ids will be changed to match new character id.
    /// </summary>
    public bool ChangeLockCamParamIds { get; }
    /// <summary>
    /// EXPERIMENTAL, may give weird results. Guesses which DS3 material would best fit a mesh using buffer layout,
    /// and amounts of texture types.
    /// </summary>
    public bool UseBestFitMaterials { get; }
    /// <summary>
    /// List of animation offsets which are excluded when porting an anibnd.
    /// </summary>
    public List<int> ExcludedAnimOffsets { get; }

    public Options(string[] args)
    {
        Cwd = AppDomain.CurrentDomain.BaseDirectory;
        SourceId = "";
        PortedId = "";
        SoundId = "";
        LockCamParamId = "";
        ExcludedAnimOffsets = new List<int>();
        OutputGame = Game.GameTypes.DarkSouls3;

        string[] contentSourceFiles = Array.FindAll(args, x => File.Exists(x) && 
                                                   Path.GetFileName(x).Contains("bnd.dcx") && !Path.GetFileName(x).Contains("texbnd.dcx"));
        
        if (contentSourceFiles.Length == 0)
        {
            throw new ArgumentException("No path to a source binder found in arguments.");
        }
        
        string[] textureSourceFiles = Array.FindAll(args, x => File.Exists(x) &&
                                                               (Path.GetFileName(x).Contains("tpf.dcx") ||
                                                                Path.GetFileName(x).Contains("texbnd.dcx")));
        
        ContentSourceFileNames = new string[contentSourceFiles.Length];
        ContentSourceFiles = new ISoulsFile[contentSourceFiles.Length];
        CurrentContentSourceFileName = ContentSourceFileNames[0];
        CurrentContentSourceFile = ContentSourceFiles[0];
        
        TextureSourceFileNames = new string[textureSourceFiles.Length];
        TextureSourceFiles = new ISoulsFile[textureSourceFiles.Length];
        if (textureSourceFiles.Length > 0)
        {
            CurrentTextureSourceFileName = TextureSourceFileNames[0];
            CurrentTextureSourceFile = TextureSourceFiles[0];
        }
        

        for (int i = 0; i < contentSourceFiles.Length; i++)
        {
            ContentSourceFileNames[i] = Path.GetFileName(contentSourceFiles[i]);
            
            if (BND4.Is(contentSourceFiles[i]))
            {
                ContentSourceFiles[i] = BND4.Read(contentSourceFiles[i]);
            }
            else if (BND3.Is(contentSourceFiles[i]))
            {
                ContentSourceFiles[i] = BND3.Read(contentSourceFiles[i]);
            }
        }
        
        for (int i = 0; i < textureSourceFiles.Length; i++)
        {
            TextureSourceFileNames[i] = Path.GetFileName(textureSourceFiles[i]);
            
            if (TPF.Is(textureSourceFiles[i]))
            {
                TextureSourceFiles[i] = TPF.Read(textureSourceFiles[i]);
            }
            else if (BND4.Is(textureSourceFiles[i]))
            {
                TextureSourceFiles[i] = BND4.Read(textureSourceFiles[i]);
            }
            else if (BND3.Is(textureSourceFiles[i]))
            {
                TextureSourceFiles[i] = BND3.Read(textureSourceFiles[i]);
            }
        }
        
        InputGame = new((IBinder)ContentSourceFiles.First(x => x is BND3 or BND4));

        string[] args1 = args;
        List<int> flagIndices = args.Where(x => Regex.IsMatch(x, @"-\w+"))
            .Select(x => Array.IndexOf(args, x))
            .Where(x => contentSourceFiles.All(y => x != Array.IndexOf(args1, y))).ToList();
		
        if (!flagIndices.Any())
        {
            Console.Write("Enter flags: ");
            string? flagString = Console.ReadLine();
            if (flagString != null)
            {
                string[] flagArgs = flagString.Split(" ");
                flagIndices = flagArgs.Where(x => Regex.IsMatch(x, @"-\w+"))
                    .Select(x => Array.IndexOf(flagArgs, x)).ToList();
                args = flagArgs.Concat(args).ToArray();
            }
        }
        
        bool isObject = false;
        
        if (ContentSourceFileNames.Any(x => x.EndsWith("geombnd.dcx")))
        {
            if (args.Any(x => x == "-obj"))
            {
                isObject = true;
            }
            else if (args.All(x => x != "-map"))
            {
                Console.Write("A geombnd was found in the input. By default it will port to a map piece.\nShould it port to an object instead? (y/n): ");
                string? answer = Console.ReadLine();
                while (answer == null || (answer != "y" && answer != "n"))
                {
                    answer = Console.ReadLine();
                }

                isObject = answer == "y";
            }
        }
        
        if (ContentSourceFileNames.Any(x => x.EndsWith("chrbnd.dcx")) || ContentSourceFileNames.Any(x => x.EndsWith("anibnd.dcx")))
        {
            SourceBndsType = AssetType.Character;
            IdLength = 4;
            SourceId = "1000";
            PortedId = "1000";
        }
        else if (ContentSourceFileNames.Any(x => x.EndsWith("objbnd.dcx")) || ContentSourceFileNames.Any(x => x.EndsWith("geombnd.dcx") && isObject))
        {
            SourceBndsType = AssetType.Object;
            IdLength = 6;
            SourceId = "100000";
            PortedId = "100000";
        }
        else if (ContentSourceFileNames.Any(x => x.EndsWith("mapbnd.dcx")) || ContentSourceFileNames.Any(x => x.EndsWith("geombnd.dcx") && !isObject))
        {
            SourceBndsType = AssetType.MapPiece;
            IdLength = 14;
            SourceId = "10000000000000";
            PortedId = "10000000000000";
        }
        else
        {
            throw new ArgumentException("One or more bnds are not of a supported type.");
        }
        
        if (Path.GetFileName(contentSourceFiles[0]).Substring(1, IdLength).All(char.IsDigit))
        {
            SourceId = Path.GetFileName(contentSourceFiles[0]).Substring(1, IdLength);
            PortedId = SourceId;
            SoundId = "";
            LockCamParamId = "";
        }

        foreach (int i in flagIndices)
        {
            if (args[i].Equals("-t"))
            {
                PortTaeOnly = true;
            }
            else if (args[i].Equals("-f"))
            {
                PortFlverOnly = true;
            }
            else if (args[i].Equals("-i"))
            {
                if (args.Length <= i + 1)
                {
                    throw new ArgumentException($"Flag '-i' used, but no id provided.");
                }
                if (args[i + 1].Length != IdLength || !args[i + 1].All(char.IsDigit))
                {
                    throw new ArgumentException($"The id after flag '-i' must be a {IdLength} digit number.");
                }

                PortedId = args[i + 1];
                if (SoundId.Equals(""))
                {
                    SoundId = PortedId;
                    ChangeSoundIds = true;
                }
                if (LockCamParamId.Equals(""))
                {
                    LockCamParamId = PortedId;
                    ChangeLockCamParamIds = true;
                }
            }
            else if (args[i].Equals("-o"))
            {
                Console.WriteLine("Flag -o is known to have bugs in this release. Use at your own discretion.");
                
                if (args.Length <= i + 1)
                {
                    throw new ArgumentException($"Flag '-o' used, but no offsets provided.");
                }
                ExcludedAnimOffsets = args[i + 1].Split(',')
                    .Where(x => x.All(char.IsDigit) && x.Length == 1)
                    .Select(Int32.Parse).ToList();
            }
            else if (args[i].Equals("-s"))
            {
                if (args.Length <= i + 1)
                {
                    ChangeSoundIds = false;
                }
                else if (flagIndices.Contains(i + 1) || contentSourceFiles.Any(x => i + 1 == Array.IndexOf(args, x)))
                {
                    ChangeSoundIds = false;
                }
                else if (args[i + 1].Length != IdLength || !args[i + 1].All(char.IsDigit))
                {
                    throw new ArgumentException($"The id after flag '-s' must be a {IdLength} digit number.");
                }
                else if (args[i + 1].Length == IdLength || args[i + 1].All(char.IsDigit))
                {
                    SoundId = args[i + 1];
                    ChangeSoundIds = true;
                }
            }
            else if (args[i].Equals("-l"))
            {
                if (args.Length <= i + 1)
                {
                    ChangeLockCamParamIds = false;
                }
                else if (flagIndices.Contains(i + 1) || contentSourceFiles.Any(x => i + 1 == Array.IndexOf(args, x)))
                {
                    ChangeLockCamParamIds = false;
                }
                else if (args[i + 1].Length != IdLength || !args[i + 1].All(char.IsDigit))
                {
                    throw new ArgumentException($"The id after flag '-l' must be a {IdLength} digit number.");
                }
                else if (args[i + 1].Length == IdLength || args[i + 1].All(char.IsDigit))
                {
                    LockCamParamId = args[i + 1];
                    ChangeLockCamParamIds = true;
                }
            }
            else if (args[i].Equals("-m"))
            {
                UseBestFitMaterials = true;
            }
            else if (args[i].Equals("-ot"))
            {
                if (i + 1 < args.Length)
                {
                    var argument = args[i + 1].ToLower();
                    switch (argument)
                    {
                        case "ds3":
                        case "darksouls3":
                            OutputGame = Game.GameTypes.DarkSouls3;
                            break;
                        case "sekiro":
                        case "sek":
                            OutputGame = Game.GameTypes.Sekiro;
                            break;
                        case "eldenring":
                        case "er":
                            OutputGame = Game.GameTypes.EldenRing;
                            break;
                        default:
                            throw new ArgumentException($"Unknown output game type: {argument}");
                    }
                }
            }
            else if (args[i].Equals("-ot"))
            {
                if (i + 1 < args.Length)
                {
                    var argument = args[i + 1].ToLower();
                    switch (argument)
                    {
                        case "bb":
                        case "bloodborne":
                            OutputGame = new Game(Game.GameTypes.Bloodborne);
                            break;
                        case "ds3":
                        case "darksouls3":
                            OutputGame = new Game(Game.GameTypes.DarkSouls3);
                            break;
                        case "sdt":
                        case "sekiro":
                            OutputGame = new Game(Game.GameTypes.Sekiro);
                            break;
                        case "eldenring":
                        case "er":
                            OutputGame = new Game(Game.GameTypes.EldenRing);
                            break;
                        case "nightreign":
                        case "nr":
                            OutputGame = new Game(Game.GameTypes.Nightreign);
                            break;
                        default:
                            throw new ArgumentException($"Unknown output game type: {argument}");
                    }
                }
            }
            else if (!(args[i].Equals("-x") || args[i].Equals("-obj") || args[i].Equals("-map")))
            {
                throw new ArgumentException($"Unknown flag: {args[i]}");
            }
        }
    }
}