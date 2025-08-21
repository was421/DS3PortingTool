using DS3PortingTool.Converter;
using SoulsFormats;

namespace DS3PortingTool;

static class Program
{
	public static void Main(string[] args)
	{
		Options op = new(args);

		Converter.Converter conv;

		switch (op.InputGame.Type)
		{
			case Game.GameTypes.Bloodborne:
				conv = new BloodborneConverter();
				break;
			case Game.GameTypes.DarkSouls3:
				conv = new DarkSouls3Converter();
				break;
			case Game.GameTypes.Sekiro:
				conv = new SekiroConverter();
				break;
			case Game.GameTypes.EldenRing:
				conv = new EldenRingConverter();
				break;
			case Game.GameTypes.Nightreign:
				conv = new NightreignConverter();
				break;
			default:
				throw new ArgumentException("The game this binder originates from is not supported.");
		}
		
		for (int i = 0; i < op.ContentSourceFiles.Length; i++)
		{
			op.CurrentContentSourceFileName = op.ContentSourceFileNames[i];
			op.CurrentContentSourceFile = op.ContentSourceFiles[i];
			
			conv.DoConversion(op);
		}
		
		for (int i = 0; i < op.TextureSourceFiles.Length; i++)
		{
			op.CurrentTextureSourceFileName = op.TextureSourceFileNames[i];
			op.CurrentTextureSourceFile = op.TextureSourceFiles[i];
			
			conv.DoConversion(op);
		}
	}
}
