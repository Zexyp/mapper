using System;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text;

// v2.0

namespace Mapper
{
    public class MapParser
    {
        const byte flagsInternalTexcoords = 1;
        const byte flagsExternalTexcoords = 2;
        const byte flagsPerVertexUndulation = 4;
        const byte flagsTextureMode = 8;

        public static MapMesh ParseMesh(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
            {
                MapMesh mesh = new MapMesh();

                // header
                if (reader.BaseStream.Length < 2)
                {
                    Log.Error("invalid stream length");
                    return null;
                }

                string magic = new string(reader.ReadChars(2));
                if (magic != "ME")
                {
                    Log.Error("file magic does not match");
                    return null;
                }

                mesh.Version = reader.ReadUInt16();

                if (mesh.Version > 3)
                {
                    Log.Error("invalid file version");
                    return null;
                }

                mesh.MeanUndulation = reader.ReadDouble();
                mesh.NumSubmeshes = reader.ReadUInt16();
                // end of header

                var submeshes = new List<MapSubmesh>(mesh.NumSubmeshes);
                mesh.GpuSize = 0;
                mesh.Faces = 0;
                mesh.Size = 0;

                for (int i = 0, li = mesh.NumSubmeshes; i < li; i++)
                {
                    MapSubmesh submesh = ParseSubmesh(mesh, reader);
                    if (submesh.Valid)
                    {
                        submeshes.Add(submesh);
                        mesh.Size += submesh.Size;
                        mesh.Faces += submesh.Faces;

                        //aproximate useless size
                        mesh.GpuSize += submesh.Size;
                    }
                }

                mesh.NumSubmeshes = (ushort)submeshes.Count;
                mesh.Submeshes = submeshes.ToArray();

                return mesh;
            }
        }

        static MapSubmesh ParseSubmesh(MapMesh mesh, BinaryReader reader)
        {
            MapSubmesh submesh = new MapSubmesh();
            submesh.Valid = true;

            ParseSubmeshHeader(mesh, submesh, reader);
            if (mesh.Version < 3 || mesh.Version > 3)
                throw new NotImplementedException("different version than 3 is not implemented!");
            ParseVerticesAndFaces2(mesh, submesh, reader);

            return submesh;
        }

        static void ParseSubmeshHeader(MapMesh mesh, MapSubmesh submesh, BinaryReader reader)
        {
            submesh.Flags = reader.ReadByte();

            if (mesh.Version > 1)
            {
                submesh.SurfaceReference = reader.ReadByte();
            }
            else
            {
                submesh.SurfaceReference = 0;
            }

            submesh.TextureLayer = reader.ReadUInt16();
            submesh.TextureLayer2 = submesh.TextureLayer; // can't belive they done this

            double[] bboxMin = new double[3];
            double[] bboxMax = new double[3];

            bboxMin[0] = reader.ReadDouble();
            bboxMin[1] = reader.ReadDouble();
            bboxMin[2] = reader.ReadDouble();

            bboxMax[0] = reader.ReadDouble();
            bboxMax[1] = reader.ReadDouble();
            bboxMax[2] = reader.ReadDouble();

            submesh.BBoxMin = bboxMin;
            submesh.BBoxMax = bboxMax;
        }

        static void ParseVerticesAndFaces2(MapMesh mesh, MapSubmesh submesh, BinaryReader reader)
        {
            byte[] byteDataBuffer = GetByteBuffer(reader);

            ushort numVertices = reader.ReadUInt16();
            ushort quant = reader.ReadUInt16();

            if (numVertices == 0)
            {
                submesh.Valid = false;
            }

            double[] center = { (submesh.BBoxMin[0] + submesh.BBoxMax[0]) / 2, (submesh.BBoxMin[1] + submesh.BBoxMax[1]) / 2, (submesh.BBoxMin[2] + submesh.BBoxMax[2]) / 2 };
            double scale = Math.Abs(Math.Max(Math.Max(submesh.BBoxMax[0] - submesh.BBoxMin[0], submesh.BBoxMax[1] - submesh.BBoxMin[1]), submesh.BBoxMax[2] - submesh.BBoxMin[2]));

            double multiplier = 1.0 / quant;

            Array externalUVs = null;
            Array internalUVs = null;

            Array vertices = Config.Map16bitMeshes ? (new ushort[numVertices * 3]) : (new float[numVertices * 3]);

            long x = 0;
            long y = 0;
            long z = 0;

            double cx = center[0];
            double cy = center[1];
            double cz = center[2];

            double mx = submesh.BBoxMin[0];
            double my = submesh.BBoxMin[1];
            double mz = submesh.BBoxMin[2];

            double sx = 1.0 / (submesh.BBoxMax[0] - submesh.BBoxMin[0]);
            double sy = 1.0 / (submesh.BBoxMax[1] - submesh.BBoxMin[1]);
            double sz = 1.0 / (submesh.BBoxMax[2] - submesh.BBoxMin[2]);

            long[] res = { 0, reader.BaseStream.Position };

            double t = 0;
            int vindex;

            if (Config.Map16bitMeshes)
            {
                for (int i = 0; i < numVertices; i++)
                {
                    ParseDelta(byteDataBuffer, res);
                    x += res[0];
                    ParseDelta(byteDataBuffer, res);
                    y += res[0];
                    ParseDelta(byteDataBuffer, res);
                    z += res[0];

                    vindex = i * 3;
                    t = ((x * multiplier * scale + cx) - mx) * sx;
                    if (t < 0) t = 0; if (t > 1.0) t = 1.0;
                    ((ushort[])vertices)[vindex] = (ushort)(t * 65535);
                    t = ((y * multiplier * scale + cy) - my) * sy;
                    if (t < 0) t = 0; if (t > 1.0) t = 1.0;
                    ((ushort[])vertices)[vindex + 1] = (ushort)(t * 65535);
                    t = ((z * multiplier * scale + cz) - mz) * sz;
                    if (t < 0) t = 0; if (t > 1.0) t = 1.0;
                    ((ushort[])vertices)[vindex + 2] = (ushort)(t * 65535);
                }
            }
            else
            {
                for (int i = 0; i < numVertices; i++)
                {
                    ParseDelta(byteDataBuffer, res);
                    x += res[0];
                    ParseDelta(byteDataBuffer, res);
                    y += res[0];
                    ParseDelta(byteDataBuffer, res);
                    z += res[0];

                    vindex = i * 3;
                    ((float[])vertices)[vindex] = (float)(((x * multiplier * scale + cx) - mx) * sx);
                    ((float[])vertices)[vindex + 1] = (float)(((y * multiplier * scale + cy) - my) * sy);
                    ((float[])vertices)[vindex + 2] = (float)(((z * multiplier * scale + cz) - mz) * sz);
                }
            }

            reader.BaseStream.Position = res[1];

            if ((submesh.Flags & flagsExternalTexcoords) > 0)
            {
                quant = reader.ReadUInt16();
                res[1] = reader.BaseStream.Position;

                if (Config.MapOnlyOneUVs)
                {

                    for (int i = 0; i < numVertices; i++)
                    {
                        ParseDelta(byteDataBuffer, res);
                        ParseDelta(byteDataBuffer, res);
                    }

                }
                else
                {
                    multiplier = Config.Map16bitMeshes ? (65535 / quant) : (1.0 / quant);
                    externalUVs = Config.Map16bitMeshes ? (new ushort[numVertices * 2]) : (new float[numVertices * 2]);
                    x = 0;
                    y = 0;

                    if (Config.Map16bitMeshes)
                    {
                        for (int i = 0; i < numVertices; i++)
                        {
                            ParseDelta(byteDataBuffer, res);
                            x += res[0];
                            ParseDelta(byteDataBuffer, res);
                            y += res[0];

                            var uvindex = i * 2;
                            t = x * multiplier;
                            if (t < 0) t = 0; if (t > 65535) t = 65535;
                            ((ushort[])externalUVs)[uvindex] = (ushort)t;
                            t = y * multiplier;
                            if (t < 0) t = 0; if (t > 65535) t = 65535;
                            ((ushort[])externalUVs)[uvindex + 1] = (ushort)(65535 - t);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numVertices; i++)
                        {
                            ParseDelta(byteDataBuffer, res);
                            x += res[0];
                            ParseDelta(byteDataBuffer, res);
                            y += res[0];

                            var uvindex = i * 2;
                            ((float[])externalUVs)[uvindex] = (float)(x * multiplier);
                            ((float[])externalUVs)[uvindex + 1] = (float)(1 - (y * multiplier));
                        }
                    }
                }
            }

            reader.BaseStream.Position = res[1];

            var tmpVertices = vertices;
            var tmpExternalUVs = externalUVs;
            Array tmpInternalUVs = null;

            if ((submesh.Flags & flagsInternalTexcoords) > 0)
            {
                ushort numUVs = reader.ReadUInt16();
                ushort quantU = reader.ReadUInt16();
                ushort quantV = reader.ReadUInt16();
                double multiplierU = Config.Map16bitMeshes ? (65536.0 / quantU) : (1.0 / quantU);
                double multiplierV = Config.Map16bitMeshes ? (65536.0 / quantV) : (1.0 / quantV);
                x = 0;
                y = 0;

                internalUVs = Config.Map16bitMeshes ? (new ushort[numUVs * 2]) : (new float[numUVs * 2]);
                res[1] = reader.BaseStream.Position;

                if (Config.Map16bitMeshes)
                {
                    for (int i = 0, li = numUVs * 2; i < li; i += 2)
                    {
                        ParseDelta(byteDataBuffer, res);
                        x += res[0];
                        ParseDelta(byteDataBuffer, res);
                        y += res[0];

                        t = x * multiplierU;
                        if (t < 0) t = 0; if (t > 65535) t = 65535;
                        ((ushort[])internalUVs)[i] = (ushort)t;
                        t = y * multiplierV;
                        if (t < 0) t = 0; if (t > 65535) t = 65535;
                        ((ushort[])internalUVs)[i + 1] = (ushort)(65535 - t);
                    }
                }
                else
                {
                    for (int i = 0, li = numUVs * 2; i < li; i += 2)
                    {
                        ParseDelta(byteDataBuffer, res);
                        x += res[0];
                        ParseDelta(byteDataBuffer, res);
                        y += res[0];

                        ((float[])internalUVs)[i] = (float)(x * multiplierU);
                        ((float[])internalUVs)[i + 1] = (float)(1 - (y * multiplierV));
                    }
                }

                reader.BaseStream.Position = res[1];

                tmpInternalUVs = internalUVs;
            }

            ushort numFaces = reader.ReadUInt16();
            ushort[] indices = null;

            internalUVs = null;
            externalUVs = null;

            bool onlyExternalIndices = (Config.MapIndexBuffers && Config.MapOnlyOneUVs && !((submesh.Flags & flagsInternalTexcoords) > 0));
            bool onlyInternalIndices = (Config.MapIndexBuffers && Config.MapOnlyOneUVs && ((submesh.Flags & flagsInternalTexcoords) > 0));
            bool onlyIndices = onlyExternalIndices || onlyInternalIndices;

            if (onlyIndices)
            {
                indices = new ushort[numFaces * 3];
            }
            else
            {
                vertices = Config.Map16bitMeshes ? (new ushort[numFaces * 3 * 3]) : (new float[numFaces * 3 * 3]);

                if ((submesh.Flags & flagsInternalTexcoords) > 0)
                {
                    internalUVs = Config.Map16bitMeshes ? (new ushort[numFaces * 3 * 2]) : (new float[numFaces * 3 * 2]);
                }

                if (!Config.MapOnlyOneUVs && ((submesh.Flags & flagsExternalTexcoords) > 0))
                {
                    externalUVs = Config.Map16bitMeshes ? (new ushort[numFaces * 3 * 2]) : (new float[numFaces * 3 * 2]);
                }
            }

            var vtmp = tmpVertices;
            var eUVs = tmpExternalUVs;
            var iUVs = tmpInternalUVs;
            var high = 0;
            long v1, v2, v3, vv1, vv2, vv3;

            res[1] = reader.BaseStream.Position;

            for (int i = 0; i < numFaces; i++)
            {
                ParseWord(byteDataBuffer, res);
                v1 = high - res[0];
                if (!(res[0] > 0)) high++;

                ParseWord(byteDataBuffer, res);
                v2 = high - res[0];
                if (!(res[0] > 0)) high++;

                ParseWord(byteDataBuffer, res);
                v3 = high - res[0];
                if (!(res[0] > 0)) high++;

                if (onlyIndices)
                {
                    vindex = i * 3;
                    indices[vindex] = (ushort)v1;
                    indices[vindex + 1] = (ushort)v2;
                    indices[vindex + 2] = (ushort)v3;
                }
                else
                {
                    throw new NotImplementedException();

                    //vindex = i * (3 * 3);
                    //long sindex = v1 * 3;
                    //vertices[vindex] = vtmp[sindex];
                    //vertices[vindex + 1] = vtmp[sindex + 1];
                    //vertices[vindex + 2] = vtmp[sindex + 2];
                    //
                    //sindex = v2 * 3;
                    //vertices[vindex + 3] = vtmp[sindex];
                    //vertices[vindex + 4] = vtmp[sindex + 1];
                    //vertices[vindex + 5] = vtmp[sindex + 2];
                    //
                    //sindex = v3 * 3;
                    //vertices[vindex + 6] = vtmp[sindex];
                    //vertices[vindex + 7] = vtmp[sindex + 1];
                    //vertices[vindex + 8] = vtmp[sindex + 2];
                    //
                    //if (externalUVs != null)
                    //{
                    //    vindex = i * (3 * 2);
                    //    externalUVs[vindex] = eUVs[v1 * 2];
                    //    externalUVs[vindex + 1] = eUVs[v1 * 2 + 1];
                    //    externalUVs[vindex + 2] = eUVs[v2 * 2];
                    //    externalUVs[vindex + 3] = eUVs[v2 * 2 + 1];
                    //    externalUVs[vindex + 4] = eUVs[v3 * 2];
                    //    externalUVs[vindex + 5] = eUVs[v3 * 2 + 1];
                    //}
                }
            }

            if (onlyExternalIndices)
            {
                vertices = tmpVertices;
                externalUVs = tmpExternalUVs;
            }

            if (onlyInternalIndices)
            {
                vertices = Config.Map16bitMeshes ? (new ushort[(iUVs.Length / 2) * 3]) : (new float[(iUVs.Length / 2) * 3]);
                internalUVs = tmpInternalUVs;
            }

            high = 0;

            if (internalUVs != null)
            {
                for (int i = 0; i < numFaces; i++)
                {
                    ParseWord(byteDataBuffer, res);
                    v1 = high - res[0];
                    if (!(res[0] > 0)) high++;
                    
                    ParseWord(byteDataBuffer, res);
                    v2 = high - res[0];
                    if (!(res[0] > 0)) high++;
                    
                    ParseWord(byteDataBuffer, res);
                    v3 = high - res[0];
                    if (!(res[0] > 0)) high++;
                    
                    if (onlyInternalIndices)
                    {
                        vindex = i * 3;
                    
                        vv1 = indices[vindex] * 3;
                        vv2 = indices[vindex + 1] * 3;
                        vv3 = indices[vindex + 2] * 3;
                        
                        if (vertices.GetType() == typeof(ushort[]) && vtmp.GetType() == typeof(ushort[]))
                        {
                            ((ushort[])vertices)[v1 * 3] = ((ushort[])vtmp)[vv1];
                            ((ushort[])vertices)[v1 * 3 + 1] = ((ushort[])vtmp)[vv1 + 1];
                            ((ushort[])vertices)[v1 * 3 + 2] = ((ushort[])vtmp)[vv1 + 2];

                            ((ushort[])vertices)[v2 * 3] = ((ushort[])vtmp)[vv2];
                            ((ushort[])vertices)[v2 * 3 + 1] = ((ushort[])vtmp)[vv2 + 1];
                            ((ushort[])vertices)[v2 * 3 + 2] = ((ushort[])vtmp)[vv2 + 2];

                            ((ushort[])vertices)[v3 * 3] = ((ushort[])vtmp)[vv3];
                            ((ushort[])vertices)[v3 * 3 + 1] = ((ushort[])vtmp)[vv3 + 1];
                            ((ushort[])vertices)[v3 * 3 + 2] = ((ushort[])vtmp)[vv3 + 2];
                        }
                        if (vertices.GetType() == typeof(float[]) && vtmp.GetType() == typeof(float[]))
                        {
                            ((float[])vertices)[v1 * 3] = ((float[])vtmp)[vv1];
                            ((float[])vertices)[v1 * 3 + 1] = ((float[])vtmp)[vv1 + 1];
                            ((float[])vertices)[v1 * 3 + 2] = ((float[])vtmp)[vv1 + 2];

                            ((float[])vertices)[v2 * 3] = ((float[])vtmp)[vv2];
                            ((float[])vertices)[v2 * 3 + 1] = ((float[])vtmp)[vv2 + 1];
                            ((float[])vertices)[v2 * 3 + 2] = ((float[])vtmp)[vv2 + 2];

                            ((float[])vertices)[v3 * 3] = ((float[])vtmp)[vv3];
                            ((float[])vertices)[v3 * 3 + 1] = ((float[])vtmp)[vv3 + 1];
                            ((float[])vertices)[v3 * 3 + 2] = ((float[])vtmp)[vv3 + 2];
                        }

                        indices[vindex] = (ushort)v1;
                        indices[vindex + 1] = (ushort)v2;
                        indices[vindex + 2] = (ushort)v3;
                    }
                    else
                    {
                        vindex = i * (3 * 2);
                        if (internalUVs.GetType() == typeof(ushort[]) && iUVs.GetType() == typeof(ushort[]))
                        {
                            ((ushort[])internalUVs)[vindex] = ((ushort[])iUVs)[v1 * 2];
                            ((ushort[])internalUVs)[vindex + 1] = ((ushort[])iUVs)[v1 * 2 + 1];
                            ((ushort[])internalUVs)[vindex + 2] = ((ushort[])iUVs)[v2 * 2];
                            ((ushort[])internalUVs)[vindex + 3] = ((ushort[])iUVs)[v2 * 2 + 1];
                            ((ushort[])internalUVs)[vindex + 4] = ((ushort[])iUVs)[v3 * 2];
                            ((ushort[])internalUVs)[vindex + 5] = ((ushort[])iUVs)[v3 * 2 + 1];
                        }
                        if (internalUVs.GetType() == typeof(float[]) && iUVs.GetType() == typeof(float[]))
                        {
                            ((float[])internalUVs)[vindex] = ((float[])iUVs)[v1 * 2];
                            ((float[])internalUVs)[vindex + 1] = ((float[])iUVs)[v1 * 2 + 1];
                            ((float[])internalUVs)[vindex + 2] = ((float[])iUVs)[v2 * 2];
                            ((float[])internalUVs)[vindex + 3] = ((float[])iUVs)[v2 * 2 + 1];
                            ((float[])internalUVs)[vindex + 4] = ((float[])iUVs)[v3 * 2];
                            ((float[])internalUVs)[vindex + 5] = ((float[])iUVs)[v3 * 2 + 1];
                        }
                    }
                }
            }

            reader.BaseStream.Position = res[1];

            submesh.Vertices = vertices;
            submesh.InternalUVs = internalUVs;
            submesh.ExternalUVs = externalUVs;
            submesh.Indices = indices;

            //tmpVertices = null;
            //tmpInternalUVs = null;
            //tmpExternalUVs = null;

            var elementSize = Config.Map16bitMeshes ? sizeof(ushort) : sizeof(float);
            submesh.Size = (uint)(elementSize * submesh.Vertices.Length);
            if (submesh.InternalUVs != null) submesh.Size += (uint)(elementSize * submesh.InternalUVs.Length);
            if (submesh.ExternalUVs != null) submesh.Size += (uint)(elementSize * submesh.ExternalUVs.Length);
            if (submesh.Indices != null) submesh.Size += (uint)(elementSize * submesh.Indices.Length);
            submesh.Faces = numFaces;
        }

        static void ParseDelta(byte[] data, long[] res)
        {
            int value = data[res[1]];

            if ((value & 128) > 0)
            {
                value = (value & 127) | (data[res[1] + 1] << 7);

                if ((value & 1) > 0)
                {
                    res[0] = -((value >> 1) + 1);
                    res[1] += 2;
                }
                else
                {
                    res[0] = (value >> 1);
                    res[1] += 2;
                }
            }
            else
            {
                if ((value & 1) > 0)
                {
                    res[0] = -((value >> 1) + 1);
                    res[1]++;
                }
                else
                {
                    res[0] = (value >> 1);
                    res[1]++;
                }
            }
        }

        static void ParseWord(byte[] data, long[] res)
        {
            int value = data[res[1]];

            if ((value & 128) > 0)
            {
                res[0] = (value & 127) | (data[res[1] + 1] << 7);
                res[1] += 2;
            }
            else
            {
                res[0] = value;
                res[1]++;
            }
        }

        static byte[] GetByteBuffer(BinaryReader reader)
        {
            long pos = reader.BaseStream.Position;
            reader.BaseStream.Position = 0;
            byte[] buffer = reader.ReadBytes((int)reader.BaseStream.Length);
            reader.BaseStream.Position = pos;
            return buffer;
        }
    }

    internal class Config
    {
        public static bool Map16bitMeshes { get; private set; } = true;
        public static bool MapOnlyOneUVs { get; private set; } = true;
        public static bool MapIndexBuffers { get; private set; } = true;
        
        //public static string BaseURL { get; private set; } = "https://mapserver-3d.mapy.cz/";
        //public static string MapConfigAddress { get; private set; } = "scenes/latest/mapConfig.json";
        //public static string MapMeshAddress { get; private set; } = "latestStage/tilesets/cities/{lod}-{x}-{y}.bin";
        //public static string MapTextureAddress { get; private set; } = "latestStage/tilesets/cities/{lod}-{x}-{y}-{sub}.jpg";
        //public static string TilesetAddress { get; private set; } = "";
    }

    /*
    default config reference

    map: null,
    mapCache: 1100,
    mapGPUCache: 600,
    mapMetatileCache: 60,
    mapTexelSizeFit: 1.1,
    mapMaxHiresLodLevels: 2,
    mapDownloadThreads: 20,
    mapMaxProcessingTime: 10,
    mapMaxGeodataProcessingTime: 10,
    mapMobileMode: !1,
    mapMobileModeAutodect: !0,
    mapMobileDetailDegradation: 1,
    mapNavSamplesPerViewExtent: 4,
    mapIgnoreNavtiles: !1,
    mapVirtualSurfaces: !0,
    mapAllowHires: !0,
    mapAllowLowres: !0,
    mapAllowSmartSwitching: !0,
    mapDisableCulling: !1,
    mapPreciseCulling: !0,
    mapHeightLodBlend: !0,
    mapHeightNodeBlend: !0,
    mapBasicTileSequence: !1,
    mapPreciseBBoxTest: !1,
    mapPreciseDistanceTest: !1,
    mapHeightfiledWhenUnloaded: !0,
    mapForceMetatileV3: !1,
    mapSmartNodeParsing: !0,
    mapLoadErrorRetryTime: 3e3,
    mapLoadErrorMaxRetryCount: 3,
    mapLoadMode: "topdown",
    mapGeodataLoadMode: "fit",
    mapSplitMeshes: !0,
    mapSplitMargin: .0025,
    mapSplitSpace: null,
    mapSplitLods: !1,
    mapGridMode: "linear",
    mapGridSurrogatez: !1,
    mapGridUnderSurface: 0,
    mapGridTextureLevel: -1,
    mapGridTextureLayer: null,
    mapXhrImageLoad: !0,
    mapStoreLoadStats: !1,
    mapRefreshCycles: 3,
    mapSoftViewSwitch: !0,
    mapSortHysteresis: !0,
    mapHysteresisWait: 0,
    mapSeparateLoader: !0,
    mapGeodataBinaryLoad: !0,
    mapPackLoaderEvents: !0,
    mapParseMeshInWorker: !0,
    mapPackGeodataEvents: !0,
    mapCheckTextureSize: !1,
    mapTraverseToMeshNode: !0,
    mapNormalizeOctantTexelSize: !0,
    mapFeatureStickMode: [1, 1],
    map16bitMeshes: !0,
    mapOnlyOneUVs: !1,
    mapIndexBuffers: !0,
    mapAsyncImageDecode: !0,
    mapFeatureGridCells: 31,
    mapFeaturesPerSquareInch: .25,
    mapFeaturesSortByTop: !1,
    mapFeaturesReduceMode: "scr-count1",
    mapFeaturesReduceParams: null,
    mapFeaturesReduceFactor: 1,
    mapFeaturesReduceFactor2: 1,
    mapDMapSize: 1024,
    mapDMapMode: 1,
    mapDegradeHorizon: !1,
    mapDegradeHorizonParams: [1, 1500, 97500, 3500],
    mapDefaultFont: "//cdn.melown.com/libs/vtsjs/fonts/noto-basic/1.0.0/noto.fnt",
    mapFog: !0,
    mapNoTextures: !1,
    mapMetricUnits: !("en" == a || 0 == a.indexOf("en-")),
    mapLanguage: a,
    mapForceFrameTime: 0,
    mapForcePipeline: 0,
    mapLogGeodataStyles: !0,
    mapBenevolentMargins: !1,
    rendererAnisotropic: 0,
    rendererAntialiasing: !0,
    rendererAllowScreenshots: !1,
    inspector: !0,
    authorization: null,
    mario: !1
    */
}
