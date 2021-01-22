using System;

using System.IO;
using System.Net;
using System.Text.Json;
using System.Drawing.Imaging;

namespace MapMess
{
    // final correctly scaled tile size is expected to be 0.5 × 0.5

    public class MapReader
    {
        BinaryReader reader;

        byte[] binaryData;

        char[] startingSequence = null;
        ushort version;
        double meanUndulation;
        ushort numSubMeshes;

        #region Current Mesh Data
        byte flags;
        byte surfaceReference;
        ushort textureLayer;
        double[] bboxMin = new double[3];
        double[] bboxMax = new double[3];
        ushort verticesCount;
        ushort secondSomething;
        #endregion

        public bool debug = false;

        public MapReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public void ReadBinary()
        {
            long pos = reader.BaseStream.Position;
            reader.BaseStream.Position = 0;
            binaryData = reader.ReadBytes((int)reader.BaseStream.Length);
            reader.BaseStream.Position = pos;
        }
        public void ReadHeader()
        {
            startingSequence = reader.ReadChars(2);
            version = reader.ReadUInt16();
            meanUndulation = reader.ReadDouble();
            numSubMeshes = reader.ReadUInt16();

            if (debug)
            {
                Console.WriteLine("me: " + new string(startingSequence));
                Console.WriteLine("version: " + version + (version < 3 ? " (not supported!)" : ""));
                Console.WriteLine("mean undulation: " + meanUndulation);
                Console.WriteLine("number of sub submeshes: " + numSubMeshes);
            }
        }
        public MapMesh ReadMesh()
        {
            if (binaryData == null)
                throw new Exception("Binary data must be read first!");
            if (startingSequence == null)
                throw new Exception("Header must be read first!");

            flags = reader.ReadByte();
            if (version >= 3) surfaceReference = reader.ReadByte(); else surfaceReference = 0;
            textureLayer = reader.ReadUInt16();
            bboxMin[0] = reader.ReadDouble();
            bboxMin[1] = reader.ReadDouble();
            bboxMin[2] = reader.ReadDouble();
            bboxMax[0] = reader.ReadDouble();
            bboxMax[1] = reader.ReadDouble();
            bboxMax[2] = reader.ReadDouble();
            verticesCount = reader.ReadUInt16();
            secondSomething = reader.ReadUInt16();

            if (debug)
            {
                Console.WriteLine("flags: " + flags);
                Console.WriteLine("surface reference: " + surfaceReference);
                Console.WriteLine("texture layer: " + textureLayer);
                Console.WriteLine("bboxMin x: " + bboxMin[0] + "; y:" + bboxMin[1] + "; z:" + bboxMin[2]);
                Console.WriteLine("bboxMax x: " + bboxMax[0] + "; y:" + bboxMax[1] + "; z:" + bboxMax[2]);
                Console.WriteLine("vertices count: " + verticesCount);
                Console.WriteLine("second something: " + secondSomething);
            }

            MapMesh n;

            n.flags = flags;
            n.surfaceReference = surfaceReference;
            n.textureLayer = textureLayer;
            n.bboxMin = bboxMin;
            n.bboxMax = bboxMax;
            n.verticesCount = verticesCount;
            n.secondSomething = secondSomething;

            bool valid = verticesCount > 0;

            ushort[] t = null;
            ushort[] r = null;
            ushort[] i = null;
            var s = reader;
            var f = (int)reader.BaseStream.Position;
            // bin data byte array
            var u = binaryData;
            // config stuff
            //      c.config.map16bitMeshes
            var x = true;
            var d = 1;
            //      c.config.mapOnlyOneUVs 
            var m = true && (flags & d) > 0;
            // vertices count?
            int b = verticesCount;
            var U = secondSomething;
            //b || (n.valid = !1);
            int vertexIndex;
            int h;
            int y;
            double p = 0.0;
            // maybe center of the mesh
            double[] V = { 0.5 * (bboxMin[0] + bboxMax[0]), 0.5 * (bboxMin[1] + bboxMax[1]), 0.5 * (bboxMin[2] + bboxMax[2]) };
            // max axis size
            double z = Math.Abs(Math.Max(bboxMax[0] - bboxMin[0], Math.Max(bboxMax[1] - bboxMin[1], bboxMax[2] - bboxMin[2])));
            double xs = bboxMax[0] - bboxMin[0];
            double ys = bboxMax[1] - bboxMin[1];
            double zs = bboxMax[2] - bboxMin[2];
            Console.WriteLine("+ fit scale: " + (xs / z) + "," + (ys / z) + "," + (zs / z));
            // some multiplier
            double F = 1.0 / U;
            ushort[] M;
            ushort[] N;
            //var L = x ? new ushort[3 * b] : new float[3 * b];
            var L = new ushort[3 * b];
            var O = 0;
            var S = 0;
            var k = 0;
            // center offsets
            var cox = V[0];
            var coy = V[1];
            var coz = V[2];

            var I = bboxMin[0];
            var P = bboxMin[1];
            var D = bboxMin[2];
            var J_ = 1.0 / (bboxMax[0] - bboxMin[0]);
            var j = 1.0 / (bboxMax[1] - bboxMin[1]);
            var E = 1.0 / (bboxMax[2] - bboxMin[2]);
            int[] T = { 0, f };
            if (x)
            {
                for (h = 0; h < b; h++)
                {
                    //Debug.Assert(p != 0);
                    o(ref u, ref T);
                    O += T[0];
                    o(ref u, ref T);
                    S += T[0];
                    o(ref u, ref T);
                    k += T[0];
                    vertexIndex = 3 * h;
                    p = (O * F * z + cox - I) * J_;
                    // clip?
                    if (p < 0) p = 0;
                    if (p > 1) p = 1;
                    L[vertexIndex] = (ushort)(65535 * p);
                    p = (S * F * z + coy - P) * j;
                    if (p < 0) p = 0;
                    if (p > 1) p = 1;
                    L[vertexIndex + 1] = (ushort)(65535 * p);
                    p = (k * F * z + coz - D) * E;
                    if (p < 0) p = 0;
                    if (p > 1) p = 1;
                    L[vertexIndex + 2] = (ushort)(65535 * p);
                }
            }
            else
            {
                for (h = 0; h < b; h++)
                {
                    o(ref u, ref T);
                    O += T[0];
                    o(ref u, ref T);
                    S += T[0];
                    o(ref u, ref T);
                    k += T[0];
                    b = 3 * h;
                    //L[vertexIndex] = (float)(O * F * z + cox - I) * J;
                    //L[vertexIndex + 1] = (float)(S * F * z + coy - P) * j;
                    //L[vertexIndex + 2] = (float)(k * F * z + coz - D) * E;
                }
            }

            //foreach(ushort el in L)
            //    Console.WriteLine(el);

            var g = 2;
            //M = x ? new Uint16Array(2 * b) : new Float32Array(2 * b)
            M = new ushort[2 * b];

            if ((flags & g) > 0)
            {
                f = T[1];
                reader.BaseStream.Position = f;

                if ((U = reader.ReadUInt16()) > 0 && (T[1] = (int)reader.BaseStream.Position) > 0 && m)
                {
                    for (h = 0; h < b; h++)
                    {
                        o(ref u, ref T);
                        o(ref u, ref T);
                    }
                }
                else if ((F = x ? 65535 / U : 1 / U) > 0 && x)
                {
                    O = 0;
                    S = 0;
                    for (h = 0; h < b; h++)
                    {
                        o(ref u, ref T);
                        O += T[0];
                        o(ref u, ref T);
                        S += T[0];
                        var q = 2 * h;
                        p = O * F;
                        if (p < 0) p = 0;
                        if (p > 65535) p = 65535;
                        M[q] = (ushort)p;
                        p = S * F;
                        if (p < 0) p = 0;
                        if (p > 65535) p = 65535;
                        M[q + 1] = (ushort)(65535 - p);
                    }
                }
                else
                {
                    for (h = 0; h < b; h++)
                    {
                        o(ref u, ref T);
                        O += T[0];
                        o(ref u, ref T);
                        S += T[0];
                        var q = 2 * h;
                        M[q] = (ushort)(O * F);
                        M[q + 1] = (ushort)(1 - S * F);
                    }
                }
            }

            //Console.WriteLine("uv?:");
            //foreach (ushort el in M)
            //    Console.WriteLine(el);

            if ((flags & d) > 0)
            {
                f = T[1];
                reader.BaseStream.Position = f;

                t = L;
                r = M;
                var H = reader.ReadUInt16();
                var G = reader.ReadUInt16();
                var X = reader.ReadUInt16();
                var J = x ? 65536 / G : 1 / G;
                var K = x ? 65536 / X : 1 / X;
                O = 0;
                S = 0;

                f = (int)reader.BaseStream.Position;

                //var N = x ? new Uint16Array(2 * H) : new Float32Array(2 * H);
                N = new ushort[2 * H];
                if (x)
                {
                    T[1] = f;
                    for (h = 0, y = 2 * H; h < y; h += 2)
                    {
                        o(ref u, ref T);
                        O += T[0];
                        o(ref u, ref T);
                        S += T[0];
                        p = O * J;
                        if (p < 0) p = 0;
                        if (p > 65535) p = 65535;
                        N[h] = (ushort)p;
                        p = S * K;
                        if (p < 0) p = 0;
                        if (p > 65535) p = 65535;
                        N[h + 1] = (ushort)(65535 - p);
                    }
                }
                else
                {
                    for (h = 0, y = 2 * H; h < y; h += 2)
                    {
                        o(ref u, ref T);
                        O += T[0];
                        o(ref u, ref T);
                        S += T[0];
                        N[h] = (ushort)(O * J);
                        N[h + 1] = (ushort)(1 - S * K);
                    }
                }
                f = T[1];
                reader.BaseStream.Position = f;
                i = N;
            }

            //Console.WriteLine("uv2?:");
            //foreach (ushort el in i)
            //    Console.WriteLine(el);

            var Q = reader.ReadUInt16();
            f = (int)reader.BaseStream.Position;
            ushort[] W = null;
            N = null;
            M = null;
            //      c.config.mapIndexBuffers    c.config.mapOnlyOneUVs
            bool Y = true && true && !((flags & d) > 0);
            bool Z = true && true && (flags & d) > 0;
            bool dols = Y || Z;

            /*
            if(dols)
            {
                W = new Uint16Array(3 * Q) 
            }
            else
            {
                (if(x)
                {
                    L = new Uint16Array(3 * Q * 3) 
                }
                else
                {
                    L = new Float32Array(3 * Q * 3),
                    if ((flags & d) > 0)
                    {
                        (if (x)
                        {
                            N = new Uint16Array(3 * Q * 2)
                        }
                        else
                        {
                            N = new Float32Array(3 * Q * 2)),
                        }
                    }
                }
                if(!m && (flags & g)>0)
                {
                    (if(x)
                    {
                        M = new Uint16Array(3 * Q * 2) 
                    }
                    else
                    {
                        M = new Float32Array(3 * Q * 2)));
                    }
                }
            }
            */
            W = new ushort[3 * Q];

            ushort ee, ne, ae, te, re, ie;
            var se = t;
            var le = r;
            var oe = i;
            var fe = 0;
            int v;
            for (T[1] = f, h = 0; h < Q; h++)
            {
                l(ref u, ref T);
                ee = (ushort)(fe - T[0]);
                if (!((T[0]) > 0)) fe++;
                l(ref u, ref T);
                ne = (ushort)(fe - T[0]);
                if (!((T[0]) > 0)) fe++;
                l(ref u, ref T);
                ae = (ushort)(fe - T[0]);
                if (!((T[0]) > 0)) fe++;
                //if (l(u, T) && (ee = fe - T[0]) > 0 && (T[0]) > 0 || (fe++) > 0 && l(u, T) && (ne = fe - T[0]) > 0 && (T[0]) > 0 || (fe++) > 0 && l(u, T) && (ae = fe - T[0]) > 0 && (T[0]) > 0 || (fe++) > 0 && dols)
                if (dols)
                {
                    v = 3 * h;
                    W[v] = ee;
                    W[v + 1] = ne;
                    W[v + 2] = ae;
                }
                else
                {
                    v = 9 * h;
                    var ue = 3 * ee;
                    L[v] = se[ue];
                    L[v + 1] = se[ue + 1];
                    L[v + 2] = se[ue + 2];
                    ue = 3 * ne;
                    L[v + 3] = se[ue];
                    L[v + 4] = se[ue + 1];
                    L[v + 5] = se[ue + 2];
                    ue = 3 * ae;
                    L[v + 6] = se[ue];
                    L[v + 7] = se[ue + 1];
                    L[v + 8] = se[ue + 2];
                    if (null != M)
                    {
                        v = 6 * h;
                        M[v] = le[2 * ee];
                        M[v + 1] = le[2 * ee + 1];
                        M[v + 2] = le[2 * ne];
                        M[v + 3] = le[2 * ne + 1];
                        M[v + 4] = le[2 * ae];
                        M[v + 5] = le[2 * ae + 1];
                    }

                }
            }

            //Console.WriteLine("indices?:");
            //foreach (ushort el in W)
            //    Console.WriteLine(el);

            if (Y)
            {
                L = t;
                M = r;
            }

            if (Z)
            {
                //if (x)
                //{
                //    L = new Uint16Array(oe.length / 2 * 3)
                //}
                //else
                //{
                //    L = new Float32Array(oe.length / 2 * 3)
                //}
                L = new ushort[oe.Length / 2 * 3];
                N = i;
            }

            fe = 0;

            if (null != N)
            {
                for (h = 0; h < Q; h++)
                {
                    l(ref u, ref T);
                    ee = (ushort)(fe - T[0]);
                    if (!((T[0]) > 0)) fe++;
                    l(ref u, ref T);
                    ne = (ushort)(fe - T[0]);
                    if (!((T[0]) > 0)) fe++;
                    l(ref u, ref T);
                    ae = (ushort)(fe - T[0]);
                    if (!((T[0]) > 0)) fe++;
                    if (Z)
                    {
                        v = 3 * h;
                        te = (ushort)(3 * W[v]);
                        re = (ushort)(3 * W[v + 1]);
                        ie = (ushort)(3 * W[v + 2]);
                        L[3 * ee] = se[te];
                        L[3 * ee + 1] = se[te + 1];
                        L[3 * ee + 2] = se[te + 2];
                        L[3 * ne] = se[re];
                        L[3 * ne + 1] = se[re + 1];
                        L[3 * ne + 2] = se[re + 2];
                        L[3 * ae] = se[ie];
                        L[3 * ae + 1] = se[ie + 1];
                        L[3 * ae + 2] = se[ie + 2];
                        W[v] = ee;
                        W[v + 1] = ne;
                        W[v + 2] = ae;
                    }
                    else
                    {
                        v = 6 * h;
                        N[v] = oe[2 * ee];
                        N[v + 1] = oe[2 * ee + 1];
                        N[v + 2] = oe[2 * ne];
                        N[v + 3] = oe[2 * ne + 1];
                        N[v + 4] = oe[2 * ae];
                        N[v + 5] = oe[2 * ae + 1];
                    }
                }
            }

            f = T[1];
            reader.BaseStream.Position = f;

            //Console.WriteLine("wtf?:");
            //foreach (ushort el in N)
            //    Console.WriteLine(el);



            n.vertices = L;
            n.internalUVs = N;
            n.externalUVs = M;
            n.indices = W;
            n.size = (uint)Buffer.ByteLength(n.vertices);
            if (n.internalUVs != null ? n.internalUVs.Length > 0 : false) n.size += (uint)Buffer.ByteLength(n.internalUVs);
            if (n.externalUVs != null ? n.externalUVs.Length > 0 : false) n.size += (uint)Buffer.ByteLength(n.externalUVs);
            if (n.indices.Length > 0) n.size += (uint)Buffer.ByteLength(n.indices);
            n.faces = Q;
            //a.index = f,

            //n.bboxMin = bboxMin;
            //n.bboxMax = bboxMax;

            return n;

            //foreach (ushort el in n.vertices)
            //    Console.Write(el+",");
        }

        //public void Clear()
        //{
        //    binaryData = null;
        //    startingSequence = null;
        //}

        //                       binaryData             limits
        void o(ref byte[] e, ref int[] n)
        {
            //Console.WriteLine("n: " + n[1]);
            int a = e[n[1]];
            //Debug.Assert(n[0] != 0);
            if ((128 & a) > 0)
            {
                a = (127 & a | e[n[1] + 1] << 7);
                if ((1 & a) > 0)
                {
                    n[0] = -(1 + (a >> 1));
                    n[1] += 2;
                }
                else
                {
                    n[0] = a >> 1;
                    n[1] += 2;
                }
            }
            else
            {
                if ((1 & a) > 0)
                {
                    n[0] = -(1 + (a >> 1));
                    n[1]++;
                }
                else
                {
                    n[0] = a >> 1;
                    n[1]++;
                }
            }
            //if ((128 & a) > 0)
            //{
            //    a = (byte)(127 & a | e[n[1] + 1] << 7);
            //    if ((1 & a) > 0)
            //        (n[0] = -(1 + (a >> 1)), n[1] += 2)
            //    else
            //        (n[0] = a >> 1, n[1] += 2))
            //}
            //else
            //{
            //    if ((1 & a) > 0)
            //        (n[0] = -(1 + (a >> 1)), n[1]++)
            //    else
            //        (n[0] = a >> 1, n[1]++)
            //}
        }

        //                       binaryData             limits
        static void l(ref byte[] e, ref int[] n)
        {
            int a = e[n[1]];
            if ((128 & a) > 0)
            {
                n[0] = 127 & a | e[n[1] + 1] << 7;
                n[1] += 2;
            }
            else
            {
                n[0] = a;
                n[1]++;
            }
        }
    }

    public struct MapMesh
    {
        public byte flags;
        public byte surfaceReference;
        public ushort textureLayer;
        public double[] bboxMin;
        public double[] bboxMax;
        public ushort verticesCount;
        public ushort secondSomething;

        public ushort[] vertices;
        public ushort[] internalUVs;
        public ushort[] externalUVs;
        public ushort[] indices;
        public uint size;
        public uint faces;
    }

    public class MapDownloader
    {
        static string urlBase = "https://mapserver-3d.mapy.cz/";
        static string config = "scenes/latest/mapConfig.json";
        //"surfaces" : "id" : "cities"
        static int[] lodRange = new int[2];
        static int[] tileRange = new int[4];
        static string meshUrl = ""; //latestStage/tilesets/cities/{lod}-{x}-{y}.bin?0
        static string textureUrl = ""; //latestStage/tilesets/cities/{lod}-{x}-{y}-{sub}.jpg?0

        public static void GetConfig()
        {
            WebRequest request = WebRequest.Create(urlBase + config);
            WebResponse response = request.GetResponse();

            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            string responseText;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream);

                responseText = reader.ReadToEnd();

                //Console.WriteLine(responseText);
            }

            response.Close();

            using (JsonDocument document = JsonDocument.Parse(responseText))
            {
                foreach (JsonElement element in document.RootElement.GetProperty("surfaces").EnumerateArray())
                {
                    if (element.GetProperty("id").GetString() == "cities")
                    {
                        int index = 0;
                        foreach (JsonElement number in element.GetProperty("lodRange").EnumerateArray())
                        {
                            lodRange[index] = number.GetInt32();
                            index++;
                        }

                        index = 0;
                        foreach (JsonElement numberArray in element.GetProperty("tileRange").EnumerateArray())
                        {
                            foreach (JsonElement number in numberArray.EnumerateArray())
                            {
                                tileRange[index] = number.GetInt32();
                                index++;
                            }
                        }

                        meshUrl = element.GetProperty("meshUrl").GetString().Replace("../", "");
                        textureUrl = element.GetProperty("textureUrl").GetString().Replace("../", "");

                        break;

                        /*
                        "lodRange" : [ 15, 22 ],
                        "meshUrl" : "../../latestStage/tilesets/cities/{lod}-{x}-{y}.bin?0",
                        "metaUrl" : "../../latestStage/tilesets/cities/{lod}-{x}-{y}.meta?0",
                        "navUrl" : "../../latestStage/tilesets/cities/{lod}-{x}-{y}.nav?0",
                        "textureUrl" : "../../latestStage/tilesets/cities/{lod}-{x}-{y}-{sub}.jpg?0",
                        "tileRange" :
                        [
                                [ 8745, 5486 ],
                                [ 9047, 5646 ]
                        ]
                        */
                    }
                }

                Console.WriteLine(lodRange[0] + "; " + lodRange[1]);
                Console.WriteLine(tileRange[0] + "; " + tileRange[1] + "; " + tileRange[2] + "; " + tileRange[3]);
                Console.WriteLine(meshUrl);
                Console.WriteLine(textureUrl);
            }
        }

        public static Stream GetMap(int lod, int x, int y)
        {
            if (!ValidateRanges(lod, x, y))
                throw new IndexOutOfRangeException("Out of range!");

            string meshRequestUrl = meshUrl;
            meshRequestUrl = meshRequestUrl.Replace("{lod}", lod.ToString());
            meshRequestUrl = meshRequestUrl.Replace("{x}", x.ToString());
            meshRequestUrl = meshRequestUrl.Replace("{y}", y.ToString());
            WebRequest request = WebRequest.Create(urlBase + meshRequestUrl);
            WebResponse response = request.GetResponse();

            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            Stream stream = response.GetResponseStream();

            response.Close();

            return stream;
        }

        public static Stream GetTextureStream(int lod, int x, int y)
        {
            if (!ValidateRanges(lod, x, y))
                throw new IndexOutOfRangeException("Out of range!");

            string textureRequestUrl = textureUrl;
            textureRequestUrl = textureRequestUrl.Replace("{lod}", lod.ToString());
            textureRequestUrl = textureRequestUrl.Replace("{x}", x.ToString());
            textureRequestUrl = textureRequestUrl.Replace("{y}", y.ToString());
            textureRequestUrl = textureRequestUrl.Replace("{sub}", 0.ToString());
            WebRequest request = WebRequest.Create(urlBase + textureRequestUrl);
            WebResponse response = request.GetResponse();

            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            Stream stream = response.GetResponseStream();

            response.Close();

            return stream;
        }

        static bool ValidateRanges(int lod, int x, int y)
        {
            if (lod < lodRange[0] && lod > lodRange[1])
                return false;
            if (x < tileRange[0] && x > tileRange[1])
                return false;
            if (y < tileRange[2] && y > tileRange[3])
                return false;
            return true;
        }
    }
}
