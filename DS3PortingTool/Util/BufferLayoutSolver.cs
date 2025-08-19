using DS3PortingTool.Util;
using SoulsFormats;

namespace FlverFixer;

public class BufferLayoutSolver
{
    public class AccessorCombination
    {
        public bool HasPosition;
        public bool HasNormal;
        public int TangentCount;
        public bool HasBiTangent;
        public int ColorCount;
        public int UVCount;
        public bool HasBones;
        public bool HasWeights;

        public static AccessorCombination GetFromVertex(FLVER.Vertex v)
        {
            AccessorCombination accessors = new AccessorCombination();
            accessors.HasPosition = Math.Abs(v.Position.X) > float.Epsilon || 
                                    Math.Abs(v.Position.Y) > float.Epsilon || 
                                    Math.Abs(v.Position.Z) > float.Epsilon;
            accessors.HasNormal = Math.Abs(v.Normal.X) > float.Epsilon || 
                                    Math.Abs(v.Normal.Y) > float.Epsilon || 
                                    Math.Abs(v.Normal.Z) > float.Epsilon;
            accessors.TangentCount = v.Tangents.Count;
            accessors.HasBiTangent = Math.Abs(v.Bitangent.X) > float.Epsilon || 
                                     Math.Abs(v.Bitangent.Y) > float.Epsilon || 
                                     Math.Abs(v.Bitangent.Z) > float.Epsilon || 
                                     Math.Abs(v.Bitangent.W) > float.Epsilon;
            accessors.ColorCount = v.Colors.Count;
            accessors.UVCount = v.UVs.Count;
            accessors.HasBones = v.BoneWeights[0] > float.Epsilon || v.BoneWeights[1] > float.Epsilon || 
                                 v.BoneWeights[2] > float.Epsilon || v.BoneWeights[3] > float.Epsilon;
            accessors.HasWeights = v.BoneWeights[0] > float.Epsilon || v.BoneWeights[1] > float.Epsilon || 
                                   v.BoneWeights[2] > float.Epsilon || v.BoneWeights[3] > float.Epsilon;

            return accessors;
        }

        public FLVER2.BufferLayout OutputBufferLayout(int streamIndex)
        {
            FLVER2.BufferLayout bufferLayout = new FLVER2.BufferLayout();
            if (HasPosition)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.Float3, FLVER.LayoutSemantic.Position));
            }
            if (HasNormal)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4, FLVER.LayoutSemantic.Normal));
            }
            if (HasBiTangent)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4, FLVER.LayoutSemantic.Bitangent));
            }
            for (int i = 0; i < TangentCount; i++)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4, FLVER.LayoutSemantic.Tangent, i));
            }
            
            if (HasBones)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4, FLVER.LayoutSemantic.BoneIndices));
            }
            if (HasWeights)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4Norm, FLVER.LayoutSemantic.BoneWeights));
            }
            
            for (int i = 0; i < ColorCount; i++)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4Norm, FLVER.LayoutSemantic.VertexColor, i + 1));
            }

            int uvIndex = 0;
            int sequenceIndex = 0;
            for (int i = 0; i < UVCount; i++)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.Float2, FLVER.LayoutSemantic.UV, i));
            }
            /*if (UVCount >= 2)
            {
                bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.Short4, FLVER.LayoutSemantic.UV));
                uvIndex += 1;
                sequenceIndex++;
                while (uvIndex < UVCount - 1)
                {
                    if (UVCount - uvIndex >= 2)
                    {
                        bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4Norm, FLVER.LayoutSemantic.UV, sequenceIndex));
                        uvIndex += 2;
                        sequenceIndex++;
                    }
                    else if (UVCount - uvIndex >= 1)
                    {
                        bufferLayout.Add(new FLVER.LayoutMember(FLVER.LayoutType.UByte4Norm, FLVER.LayoutSemantic.UV, sequenceIndex));
                        uvIndex += 1;
                    }
                }
            }*/
            return bufferLayout;
        }
        
        protected bool Equals(AccessorCombination other)
        {
            return HasPosition == other.HasPosition &&
                   HasNormal == other.HasNormal && 
                   TangentCount == other.TangentCount &&
                   HasBiTangent == other.HasBiTangent && 
                   ColorCount == other.ColorCount &&
                   UVCount == other.UVCount && 
                   HasBones == other.HasBones &&
                   HasWeights == other.HasWeights;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HasPosition, HasNormal, TangentCount, HasBiTangent, ColorCount, UVCount, HasBones, HasWeights);
        }
    }

    public void SolveBufferLayouts(FLVER2 flver)
    {
        foreach (FLVER2.Mesh? mesh in flver.Meshes)
        {
            List<AccessorCombination> accessorCombos = mesh.Vertices.Select(AccessorCombination.GetFromVertex).DistinctBy(Equals).ToList();
            int streamIndex = 0;
            List<FLVER2.BufferLayout> bufferLayouts = accessorCombos.Select(x => x.OutputBufferLayout(streamIndex++)).ToList();
                
            mesh.Vertices = mesh.Vertices.Select(x => x.Pad(bufferLayouts)).ToList();
            List<int> layoutIndices = flver.GetLayoutIndices(bufferLayouts);
            mesh.VertexBuffers = layoutIndices.Select(x =>
            {
                FLVER2.VertexBuffer vb = new FLVER2.VertexBuffer(x);
                return new FLVER2.VertexBuffer(x);
            }).ToList();
        }
    }
}