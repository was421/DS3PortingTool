using System.Numerics;
using FLVERMaterialHelper.MatShaderInfoBank;
using SoulsFormats;

namespace DS3PortingTool.Util;

public class TextureInfo
{
    public MATBIN? Material;
    public Dictionary<string,FLVER2.Texture> AlbedoTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> SpecularTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> ShininessTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> NormalTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> DisplacementTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> DetailNormalTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> BloodMaskTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> BlendMaskTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> EmissiveTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> ScatteringMaskTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> FlowTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> HighlightTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> MaskTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> BlendEdgeTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> OpacityTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> RNMTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> SnowHeightTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> BlendTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> BurningTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> DamagedNormalTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> VectorTextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> GITextures { get; } = new();
    public Dictionary<string,FLVER2.Texture> AlphaTextures { get; } = new();
    public bool IsMetallicPBR { get; set; }

    public TextureInfo(FLVER2.Material material, Dictionary<string,MATBIN>? matbins = null)
    {
        if (matbins != null)
        {
            Material = matbins.FirstOrDefault(x => x.Key.Equals(material.MTD.ToLower())).Value;
            foreach (MATBIN.Sampler sampler in Material.Samplers)
            {
                GetTextureType(sampler.Type, out bool isMetallicPBR)?.Add(sampler.Type,
                    new FLVER2.Texture(sampler.Type, sampler.Path, Vector2.One, 
                        0,0,0,0,0));
                if (isMetallicPBR && !IsMetallicPBR) IsMetallicPBR = true;
            }
        }
        else
        {
            foreach (FLVER2.Texture texture in material.Textures)
            {
                GetTextureType(texture.ParamName, out bool isMetallicPBR)?.Add(texture.ParamName,texture);
                if (isMetallicPBR && !IsMetallicPBR) IsMetallicPBR = true;
            }
        }
    }

    public Dictionary<string,FLVER2.Texture>? GetTextureType(string paramName, out bool isMetallicPBR)
    {
        isMetallicPBR = false;
        string[] splits = paramName.Split('_').Where(x => x.Length > 0).ToArray();
        string texType = int.TryParse(splits[^1], out int _) ? splits[^2] : splits[^1];
        
        return GetTextureType_ER(texType, out isMetallicPBR);
        
        return null;
    }

    private Dictionary<string,FLVER2.Texture>? GetTextureType_ER(string split, out bool isMetallicPBR)
    {
        isMetallicPBR = false;
        
        switch (split)
        {
            case "AlbedoMap":
            case "DiffuseTexture":
            case "DiffuseTexture2":
            case "DiffuseTexture3":
                return AlbedoTextures;
            case "MetallicMap":
                IsMetallicPBR = true;
                return SpecularTextures;
            case "ReflectanceMap":
            case "SpecularTexture":
            case "SpecularTexture2":
            case "SpecularTexture3":
                return SpecularTextures;
            case "ShininessMap":
            case "ShininessTexture":
            case "ShininessTexture2":
            case "ShininessTexture3":
                return ShininessTextures;
            case "NormalMap":
            case "BumpmapTexture":
            case "BumpmapTexture2":
            case "BumpmapTexture3":
            case "BumpmapTexture4":
                return NormalTextures;
            case "DetailBumpmapTexture":
            case "DetailBumpmapTexture2":
                return DetailNormalTextures;
            case "DisplacementMap":
            case "DisplacementTexture":
                return DisplacementTextures;
            case "BloodMaskTexture":
                return BloodMaskTextures;
            case "BlendMaskTexture":
            case "BlendMask":
                return BlendMaskTextures;
            case "EmissiveTexture":
            case "EmissiveTexture2":
            case "EmissiveMap":
            case "EmissiveMask":
                return EmissiveTextures;
            case "ScatteringMaskTexture":
            case "SSSMask":
                return ScatteringMaskTextures;
            case "FlowTexture":
            case "FlowMap":
                return FlowTextures;
            case "HighlightTexture":
                return HighlightTextures;
            case "MaskTexture":
            case "Mask":
                return MaskTextures;
            case "BlendEdgeTexture":
            case "BlendEdgeTexture2":
            case "BlendEdge":
                return BlendEdgeTextures;
            case "UniqueOpacityMask":
            case "OpacityTexture":
            case "OpacityTexture2":
            case "OpacityTexture3":
                return OpacityTextures;
            case "RNMTexture1":
            case "RNMTexture2":
            case "RNMTexture3":
                return RNMTextures;
            case "SnowHeightTexture":
                return SnowHeightTextures;
            case "BlendMap":
                return BlendTextures;
            case "BurningMap":
                return BurningTextures;
            case "DamageNormal":
            case "DamagedNormalTexture":
            case "DamagedNormalTexture2":
                return DamagedNormalTextures;
            case "VectorMap":
            case "VectorTexture":
                return VectorTextures;
            case "GITexture":
                return GITextures;
            case "Alpha":
                return AlphaTextures;
        }

        return null;
    }
    
    public bool CheckIfMatDefIsValid(MatShaderInfoBank.MaterialInfo matInfo)
    {
        int albedoCount = 0;
        int specularCount = 0;
        int shininessCount = 0;
        int normalCount = 0;
        int detailNormalCount = 0;
        int displacementCount = 0;
        int bloodMaskCount = 0;
        int blendMaskCount = 0;
        int emissiveCount = 0;
        int sssCount = 0;
        int flowCount = 0;
        int highlightCount = 0;
        int maskCount = 0;
        int blendEdgeCount = 0;
        int opacityCount = 0;
        int rnmCount = 0;
        int snowHeightCount = 0;
        int blendCount = 0;
        int burningCount = 0;
        int damagedNormalCount = 0;
        int vectorCount = 0;
        int giCount = 0;
        int alphaCount = 0;
        
        foreach (MatShaderInfoBank.MaterialInfo.TextureChannel channel in matInfo.TextureChannels)
        {
            string[] splits = channel.ParamName.Split('_').Where(x => x.Length > 0).ToArray();
            string texType = int.TryParse(splits[^1], out int _) ? splits[^2] : splits[^1];
            
            switch (texType)
            {
                case "AlbedoMap":
                case "DiffuseTexture":
                case "DiffuseTexture2":
                case "DiffuseTexture3":
                    albedoCount++;
                    break;
                case "MetallicMap":
                case "ReflectanceMap":
                case "SpecularTexture":
                case "SpecularTexture2":
                case "SpecularTexture3":
                    specularCount++;
                    break;
                case "ShininessMap":
                case "ShininessTexture":
                case "ShininessTexture2":
                case "ShininessTexture3":
                    shininessCount++;
                    break;
                case "NormalMap":
                case "BumpmapTexture":
                case "BumpmapTexture2":
                case "BumpmapTexture3":
                case "BumpmapTexture4":
                    normalCount++;
                    break;
                case "DetailBumpmapTexture":
                case "DetailBumpmapTexture2":
                    detailNormalCount++;
                    break;
                case "DisplacementMap":
                case "DisplacementTexture":
                    displacementCount++;
                    break;
                case "BloodMaskTexture":
                    bloodMaskCount++;
                    break;
                case "BlendMaskTexture":
                case "BlendMask":
                    blendMaskCount++;
                    break;
                case "EmissiveTexture":
                case "EmissiveTexture2":
                case "EmissiveMap":
                case "EmissiveMask":
                    emissiveCount++;
                    break;
                case "ScatteringMaskTexture":
                case "SSSMask":
                    sssCount++;
                    break;
                case "FlowTexture":
                case "FlowMap":
                    flowCount++;
                    break;
                case "HighlightTexture":
                    highlightCount++;
                    break;
                case "MaskTexture":
                case "Mask":
                    maskCount++;
                    break;
                case "BlendEdgeTexture":
                case "BlendEdgeTexture2":
                case "BlendEdge":
                    blendEdgeCount++;
                    break;
                case "UniqueOpacityMask":
                case "OpacityTexture":
                case "OpacityTexture2":
                case "OpacityTexture3":
                    opacityCount++;
                    break;
                case "RNMTexture1":
                case "RNMTexture2":
                case "RNMTexture3":
                    rnmCount++;
                    break;
                case "SnowHeightTexture":
                    snowHeightCount++;
                    break;
                case "BlendMap":
                    blendCount++;
                    break;
                case "BurningMap":
                    burningCount++;
                    break;
                case "DamageNormal":
                case "DamagedNormalTexture":
                case "DamagedNormalTexture2":
                    damagedNormalCount++;
                    break;
                case "VectorMap":
                case "VectorTexture":
                    vectorCount++;
                    break;
                case "GITexture":
                    giCount++;
                    break;
                case "Alpha":
                    alphaCount++;
                    break;
            }
        }
        
        if (albedoCount < AlbedoTextures.Count) return false;
        if (specularCount < SpecularTextures.Count) return false;
        if (shininessCount < ShininessTextures.Count) return false;
        if (normalCount < NormalTextures.Count) return false;
        if (detailNormalCount < DetailNormalTextures.Count) return false;
        if (displacementCount < DisplacementTextures.Count) return false;
        if (bloodMaskCount < BloodMaskTextures.Count) return false;
        if (blendMaskCount < BlendMaskTextures.Count) return false;
        if (emissiveCount < EmissiveTextures.Count || emissiveCount > 0 && EmissiveTextures.Count == 0) return false;
        if (sssCount < ScatteringMaskTextures.Count) return false;
        if (flowCount < FlowTextures.Count) return false;
        if (highlightCount < HighlightTextures.Count) return false;
        if (maskCount < MaskTextures.Count) return false;
        if (blendEdgeCount < BlendEdgeTextures.Count) return false;
        if (opacityCount < OpacityTextures.Count) return false;
        if (rnmCount < RNMTextures.Count) return false;
        if (snowHeightCount < SnowHeightTextures.Count) return false;
        if (blendCount < BlendTextures.Count) return false;
        if (burningCount < BurningTextures.Count) return false;
        if (damagedNormalCount < DamagedNormalTextures.Count) return false;
        if (vectorCount < VectorTextures.Count) return false;
        if (giCount < GITextures.Count) return false;
        if (alphaCount < AlphaTextures.Count) return false;

        return true;
    }
}