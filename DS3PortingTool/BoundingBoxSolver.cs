using System.Numerics;
using SoulsFormats;

namespace DS3PortingTool
{
    /// <summary>
    /// From Nordgaren's Bounding Box Patch Calculator. (Thank you Nord)
    /// </summary>
    public static class BoundingBoxSolver
    {
        //Meowmaritus showed me the way
        public static void FixAllBoundingBoxes(FLVER2 flver)
        {
            flver.Header.BoundingBoxMin = new System.Numerics.Vector3();
            flver.Header.BoundingBoxMax = new System.Numerics.Vector3();
            foreach (FLVER.Node node in flver.Nodes)
            {
                node.BoundingBoxMin = new System.Numerics.Vector3();
                node.BoundingBoxMax = new System.Numerics.Vector3();
            }

            for (int i = 0; i < flver.Meshes.Count; i++)
            {

                FLVER2.Mesh mesh = flver.Meshes[i];
                if (mesh.BoundingBox != null)
                    mesh.BoundingBox = new FLVER2.Mesh.BoundingBoxes();

                foreach (FLVER.Vertex vertex in mesh.Vertices)
                {
                    flver.Header.UpdateBoundingBox(vertex.Position);
                    if (mesh.BoundingBox != null)
                        mesh.UpdateBoundingBox(vertex.Position);

                    for (int j = 0; j < vertex.BoneIndices.Length; j++)
                    {
                        var nodeIndex = vertex.BoneIndices[j];
                        var boneDoesNotExist = false;

                        // Mark bone as not-dummied-out since there is geometry skinned to it.
                        if (nodeIndex >= 0 && nodeIndex < flver.Nodes.Count)
                        {
                            flver.Nodes[nodeIndex].Flags = 0;
                        }
                        else
                        {
                            boneDoesNotExist = true;
                        }

                        if (!boneDoesNotExist)
                            flver.Nodes[nodeIndex].UpdateBoundingBox(flver.Nodes, vertex.Position);
                    }
                }

            }
        }
        
        public static void UpdateBoundingBox(this FLVER2.FLVERHeader header, Vector3 position)
        {
            float minX = Math.Min(header.BoundingBoxMin.X, position.X);
            float minY = Math.Min(header.BoundingBoxMin.Y, position.Y);
            float minZ = Math.Min(header.BoundingBoxMin.Z, position.Z);
            float maxX = Math.Max(header.BoundingBoxMax.X, position.X);
            float maxY = Math.Max(header.BoundingBoxMax.Y, position.Y);
            float maxZ = Math.Max(header.BoundingBoxMax.Z, position.Z);
            header.BoundingBoxMin = new Vector3(minX, minY, minZ);
            header.BoundingBoxMax = new Vector3(maxX, maxY, maxZ);
        }
        
        public static void UpdateBoundingBox(this FLVER2.Mesh mesh, Vector3 position)
        {
            float minX = Math.Min(mesh.BoundingBox.Min.X, position.X);
            float minY = Math.Min(mesh.BoundingBox.Min.Y, position.Y);
            float minZ = Math.Min(mesh.BoundingBox.Min.Z, position.Z);
            float maxX = Math.Max(mesh.BoundingBox.Max.X, position.X);
            float maxY = Math.Max(mesh.BoundingBox.Max.Y, position.Y);
            float maxZ = Math.Max(mesh.BoundingBox.Max.Z, position.Z);
            mesh.BoundingBox.Min = new Vector3(minX, minY, minZ);
            mesh.BoundingBox.Max = new Vector3(maxX, maxY, maxZ);
        }
        
        public static void UpdateBoundingBox(this FLVER.Node node, List<FLVER.Node> nodes, Vector3 position)
        {
            float minX = Math.Min(node.BoundingBoxMin.X, position.X);
            float minY = Math.Min(node.BoundingBoxMin.Y, position.Y);
            float minZ = Math.Min(node.BoundingBoxMin.Z, position.Z);
            float maxX = Math.Max(node.BoundingBoxMax.X, position.X);
            float maxY = Math.Max(node.BoundingBoxMax.Y, position.Y);
            float maxZ = Math.Max(node.BoundingBoxMax.Z, position.Z);
            node.BoundingBoxMin = new Vector3(minX, minY, minZ);
            node.BoundingBoxMax = new Vector3(maxX, maxY, maxZ);
        }
    }
}