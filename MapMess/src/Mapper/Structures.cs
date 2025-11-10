using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper
{
    public class MapMesh
    {
        public ushort Version;
        public double MeanUndulation;
        public ushort NumSubmeshes;
        public MapSubmesh[] Submeshes;
        public uint Size;
        public uint Faces;
        public uint GpuSize; // useless
    }

    public class MapSubmesh
    {
        public bool Valid;
        public byte Flags;
        public byte SurfaceReference;
        public ushort TextureLayer;
        public ushort TextureLayer2;
        public double[] BBoxMin;
        public double[] BBoxMax;
        public uint Size;
        public uint Faces;
        public Array Vertices; // ushort[], float[]
        public Array InternalUVs; // ushort[], float[]
        public Array ExternalUVs; // ushort[], float[]
        public Array Indices; // ushort[] (i guess?)
    }

    public class MapConfig
    {
        public string Root;
        public MapConfigSurface[] Surfaces;
    }

    public class MapConfigSurface
    {
        public string Id;
        public int[] LodRange;
        public string MeshUrl;
        public string TextureUrl;
        public int[][] TileRange;
    }
}
