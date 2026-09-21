using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeswaxCoating
{
    /// <summary>
    /// Builds every visual asset for the mod procedurally in code: the wax chunk
    /// mesh, its textured material, and the inventory icon sprite. No vanilla
    /// prefab is cloned or reused. The icon is generated on all peers (dedicated
    /// servers included) because item pickup validation requires a non-empty
    /// icon array; the 3D visuals are only attached on rendering peers.
    /// </summary>
    internal static class WaxAssets
    {
        public static void AttachVisuals(GameObject prefab, Color wax, Color cell, int seed)
        {
            Mesh mesh = CreateWaxMesh(seed);
            Texture2D texture = CreateWaxTexture(wax, cell, seed + 1);
            Material material = CreateWaxMaterial(texture);

            MeshFilter filter = prefab.GetComponent<MeshFilter>();
            if (filter != null)
            {
                filter.sharedMesh = mesh;
            }
            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>A chunk of broken honeycomb: a cluster of jittered hexagonal prisms.</summary>
        public static Mesh CreateWaxMesh(int seed)
        {
            var rng = new System.Random(seed);
            var verts = new List<Vector3>(150);
            var uvs = new List<Vector2>(150);
            var tris = new List<int>(300);

            AddHexPrism(verts, uvs, tris, rng, new Vector3(0f, 0f, 0f), 0.23f, 0.13f, 0.1f, 0.10f);
            AddHexPrism(verts, uvs, tris, rng, new Vector3(0.14f, 0.10f, 0.05f), 0.12f, 0.19f, 0.6f, 0.16f);
            AddHexPrism(verts, uvs, tris, rng, new Vector3(-0.13f, 0.09f, -0.06f), 0.09f, 0.15f, -0.4f, 0.18f);

            var mesh = new Mesh { name = "BeeswaxCoating_WaxChunk" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddHexPrism(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            System.Random rng, Vector3 center, float radius, float height, float angleOffset, float jitter)
        {
            int b = verts.Count;
            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? 0f : height;
                for (int i = 0; i < 6; i++)
                {
                    float a = angleOffset + i * (Mathf.PI / 3f);
                    float r = radius * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter);
                    float x = Mathf.Cos(a) * r;
                    float z = Mathf.Sin(a) * r;
                    verts.Add(center + new Vector3(x, y, z));
                    uvs.Add(new Vector2(x / (radius * 4f) + 0.5f, z / (radius * 4f) + 0.5f));
                }
            }
            int t = b + 6;
            // top cap (wind so the face points up), bottom cap, then the six sides
            for (int i = 1; i <= 4; i++)
            {
                tris.Add(b); tris.Add(b + i + 1); tris.Add(b + i);
            }
            for (int i = 1; i <= 4; i++)
            {
                tris.Add(t); tris.Add(t + i); tris.Add(t + i + 1);
            }
            for (int i = 0; i < 6; i++)
            {
                int bn = b + (i + 1) % 6;
                int tn = t + (i + 1) % 6;
                tris.Add(b + i); tris.Add(bn); tris.Add(tn);
                tris.Add(b + i); tris.Add(tn); tris.Add(t + i);
            }
        }

        /// <summary>Amber wax texture with a honeycomb cell pattern and speckles.</summary>
        public static Texture2D CreateWaxTexture(Color wax, Color cell, int seed)
        {
            const int size = 128;
            var rng = new System.Random(seed);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "BeeswaxCoating_WaxTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            var pixels = new Color[size * size];

            const float hexSize = 17f;
            float hexW = Mathf.Sqrt(3f) * hexSize;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x, fy = y;
                    // nearest honeycomb center (offset rows)
                    float rowH = hexSize * 1.5f;
                    int row = Mathf.RoundToInt(fy / rowH);
                    float cx = Mathf.Round((fx - (row % 2 == 0 ? 0f : hexW * 0.5f)) / hexW) * hexW
                               + (row % 2 == 0 ? 0f : hexW * 0.5f);
                    float cy = row * rowH;
                    float dx = Mathf.Abs(fx - cx) / hexW;
                    float dy = Mathf.Abs(fy - cy) / hexSize;
                    float dist = dy + Mathf.Max(dx - 0.5f, 0f);
                    Color c = wax;
                    if (dist > 0.86f)
                    {
                        c = Color.Lerp(cell, wax, 0.25f);
                    }
                    else if (dist > 0.78f)
                    {
                        c = Color.Lerp(wax, cell, 0.6f);
                    }
                    else
                    {
                        // per-cell brightness variation + speckle
                        float cellHash = Hash(cx * 13.37f + cy * 7.77f);
                        c *= 0.92f + cellHash * 0.14f;
                        if (rng.NextDouble() < 0.02)
                        {
                            c *= 0.85f;
                        }
                    }
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Inventory icon: two overlapping hex chunks with cells, highlight and shadow.</summary>
        public static Sprite CreateWaxIcon(Color wax, Color cell)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "BeeswaxCoating_WaxIcon",
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = (x - 31.5f) / 24f;
                    float fy = (30.5f - y) / 24f;
                    float big = HexDist(fx, fy, 0.05f);
                    float sfx = (x - 45f) / 13f;
                    float sfy = (44f - y) / 13f;
                    float small = HexDist(sfx, sfy, 0.35f);

                    Color c;
                    if (big < 1f || small < 1f)
                    {
                        bool inSmall = small < big;
                        float d = inSmall ? small : big;
                        float shade = inSmall ? 1.12f : 1f;
                        // vertical gradient + top highlight
                        shade *= 0.85f + 0.3f * ((float)y / size);
                        c = Color.Lerp(wax, cell, Mathf.Clamp01((d - 0.55f) * 2.2f)) * shade;
                        // faint cell lines on the big chunk
                        if (!inSmall && ((x % 12 < 1.4f) ^ (y % 14 < 1.4f)))
                        {
                            c = Color.Lerp(c, cell, 0.35f);
                        }
                        if (d > 0.93f)
                        {
                            c = cell * 0.55f; // outline
                        }
                        pixels[y * size + x] = ClampColor(c);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>SDF-style distance to a hexagon (rotated by <paramref name="rot"/>,
        /// roughly 0.87 at the edge for a unit circumradius).</summary>
        private static float HexDist(float x, float y, float rot)
        {
            float best = 0f;
            for (int i = 0; i < 6; i++)
            {
                float a = rot + i * (Mathf.PI / 3f);
                float d = x * Mathf.Cos(a) + y * Mathf.Sin(a);
                if (d > best)
                {
                    best = d;
                }
            }
            return best;
        }

        private static float Hash(float v)
        {
            double s = Math.Sin(v * 12.9898) * 43758.5453;
            return (float)(s - Math.Floor(s));
        }

        private static Color ClampColor(Color c)
        {
            return new Color(
                Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), Mathf.Clamp01(c.a));
        }

        /// <summary>
        /// Picks a shader at runtime. The candidates are tried in order; the first
        /// present one wins and the choice is logged. The sprite shaders are Unity
        /// built-ins always included in the build, so a visible material is
        /// virtually guaranteed; if somehow none resolve, null is returned and the
        /// renderer is left without a material rather than risking a bad constructor.
        /// </summary>
        public static Material CreateWaxMaterial(Texture2D texture)
        {
            string[] candidates =
            {
                "Custom/Standard",
                "Sprites/Diffuse",
                "Sprites/Default",
                "Unlit/Texture"
            };
            foreach (var name in candidates)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                {
                    BeeswaxCoatingPlugin.Logger.LogInfo($"Wax material shader: {name}");
                    var material = new Material(shader) { mainTexture = texture };
                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", Color.white);
                    }
                    return material;
                }
            }
            BeeswaxCoatingPlugin.Logger.LogWarning("No shader found for wax material; item will render without one");
            return null;
        }
    }
}
