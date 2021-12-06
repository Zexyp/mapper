using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper
{
    public class MapMesh
    {
        public ushort version;
        public double meanUndulation;
        public ushort numSubmeshes;
        public MapSubmesh[] submeshes;
        public uint size;
        public uint faces;
        public uint gpuSize; // useless
    }

    public class MapSubmesh
    {
        public bool valid;
        public byte flags;
        public byte surfaceReference;
        public ushort textureLayer;
        public ushort textureLayer2;
        public double[] bboxMin;
        public double[] bboxMax;
        public uint size;
        public uint faces;
        public object vertices; // ushort[], float[]
        public object internalUVs; // ushort[], float[]
        public object externalUVs; // ushort[], float[]
        public object indices; // ushort[] (i guess?)
    }
}
