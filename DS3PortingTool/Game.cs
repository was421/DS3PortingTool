using SoulsFormats;

namespace DS3PortingTool;

public class Game
{
    /// <summary>
    /// The different types of games that assets can be ported from using the tool.
    /// </summary>
    public enum GameTypes
    {
        Other,

        DarkSoulsRemastered,
        
        Bloodborne,
        
        DarkSouls3,
        
        Sekiro,
        
        EldenRing,
        
        Nightreign
    }

    public enum HavokVersionType
    {
        PC_2014,
        PS4_2014,
        PC_2016,
        PC_2018
    }

    public enum BinderVersionType
    {
        BND3,
        BND4
    }

    /// <summary>
    /// The game that the source binder originates from.
    /// </summary>
    public GameTypes Type { get; }
    
    /// <summary>
    /// Which version of havok the game uses.
    /// </summary>
    public HavokVersionType HavokVersion { get; }
    
    public BinderVersionType BinderVersion { get; }

    /// <summary>
    /// The name of the game the source binder originates from, used in extracting XmlData.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The base animation offset used by the game.
    /// </summary>
    public int Offset { get; }

    public string BinderFileNameRoot { get; }
    
    
    public Game(IBinder bnd)
    {
        if (bnd.Files.Any(x => x.Name.Contains(@"N:\FRPG\data")))
        {
            Type = GameTypes.DarkSoulsRemastered;
            Name = "DarkSouls";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2016;
            BinderVersion = BinderVersionType.BND3;
            BinderFileNameRoot = @"N:\FRPG\data";
        }
        else if (bnd.Files.Any(x => x.Name.Contains(@"N:\SPRJ\data\INTERROOT_ps4")))
        {
            Type = GameTypes.Bloodborne;
            Name = "Bloodborne";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PS4_2014;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\SPRJ\data\INTERROOT_ps4";
        }
        else if (bnd.Files.Any(x => x.Name.Contains(@"N:\FDP\data\INTERROOT_win64")))
        {
            Type = GameTypes.DarkSouls3;
            Name = "DarkSouls3";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2014;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\FDP\data\INTERROOT_win64";
        }
        else if (bnd.Files.Any(x => x.Name.Contains(@"N:\NTC\data\Target\INTERROOT_win64")))
        {
            Type = GameTypes.Sekiro;
            Name = "Sekiro";
            Offset = 100000000;
            HavokVersion = HavokVersionType.PC_2016;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\NTC\data\Target\INTERROOT_win64";
        }
        else if (bnd.Files.Any(x => x.Name.Contains(@"N:\GR\data\INTERROOT_win64")))
        {
            Type = GameTypes.EldenRing;
            Name = "EldenRing";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2018;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\GR\data\INTERROOT_win64";
        }
        else if (bnd.Files.Any(x => x.Name.Contains(@"W:\CL\data\Target\INTERROOT_win64")))
        {
            Type = GameTypes.Nightreign;
            Name = "Nightreign";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2018;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"W:\CL\data\Target\INTERROOT_win64";
        }
        else
        {
            Type = GameTypes.Other;
            Name = "";
            Offset = -1;
        }
    }

    public Game(GameTypes type)
    {
        if (type == GameTypes.DarkSoulsRemastered)
        {
            Type = GameTypes.DarkSoulsRemastered;
            Name = "DarkSouls";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2016;
            BinderVersion = BinderVersionType.BND3;
            BinderFileNameRoot = @"N:\FRPG\data";
        }
        else if (type == GameTypes.Bloodborne)
        {
            Type = GameTypes.Bloodborne;
            Name = "Bloodborne";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PS4_2014;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\SPRJ\data\INTERROOT_ps4";
        }
        else if (type == GameTypes.DarkSouls3)
        {
            Type = GameTypes.DarkSouls3;
            Name = "DarkSouls3";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2014;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\FDP\data\INTERROOT_win64";
        }
        else if (type == GameTypes.Sekiro)
        {
            Type = GameTypes.Sekiro;
            Name = "Sekiro";
            Offset = 100000000;
            HavokVersion = HavokVersionType.PC_2016;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\NTC\data\Target\INTERROOT_win64";
        }
        else if (type == GameTypes.EldenRing)
        {
            Type = GameTypes.EldenRing;
            Name = "EldenRing";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2018;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"N:\GR\data\INTERROOT_win64";
        }
        else if (type == GameTypes.Nightreign)
        {
            Type = GameTypes.Nightreign;
            Name = "Nightreign";
            Offset = 1000000;
            HavokVersion = HavokVersionType.PC_2018;
            BinderVersion = BinderVersionType.BND4;
            BinderFileNameRoot = @"W:\CL\data\Target\INTERROOT_win64";
        }
        else
        {
            Type = GameTypes.Other;
            Name = "";
            Offset = -1;
        }
    }
}
