﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniGLTF
{
    public class TransformWithSkin
    {
        public Transform Transform;
        public GameObject GameObject { get { return Transform.gameObject; } }
        public int? SkinIndex;
    }

    public delegate Material CreateMaterialFunc(ImporterContext ctx, int i);

    public class ImporterContext
    {
        #region Source
        String m_path;
        public String Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
                m_prefabPath = GetPrefabPath();
            }
        }
        public String Json; // source
        public glTF GLTF; // parsed
        #endregion

        public CreateMaterialFunc CreateMaterial;

        #region Imported
        public GameObject Root;
        public List<Transform> Nodes = new List<Transform>();
        public List<TextureItem> Textures = new List<TextureItem>();
        public List<Material> Materials = new List<Material>();
        public List<MeshWithMaterials> Meshes = new List<MeshWithMaterials>();
        public AnimationClip Animation;
        #endregion

        string m_prefabPath;
        string PrefabPath
        {
            get { return m_prefabPath; }
        }
        protected virtual string GetPrefabPath()
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);
            var prefabPath = string.Format("{0}/{1}.prefab", dir, name);
#if UNITY_EDITOR
            if (!Application.isPlaying && File.Exists(prefabPath))
            {
                // already exists
                if (IsOwn(prefabPath))
                {
                    //Debug.LogFormat("already exist. own: {0}", prefabPath);
                }
                else
                {
                    // but unknown prefab
                    var unique = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                    //Debug.LogFormat("already exist: {0} => {1}", prefabPath, unique);
                    prefabPath = unique;
                }
            }
#endif
            return prefabPath;
        }

#if UNITY_EDITOR
        #region Assets
        IEnumerable<UnityEngine.Object> GetSubAssets(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path);
        }

        protected virtual bool IsOwn(string path)
        {
            foreach (var x in GetSubAssets(path))
            {
                //if (x is Transform) continue;
                if (x is GameObject) continue;
                if (x is Component) continue;
                if (AssetDatabase.IsSubAsset(x))
                {
                    return true;
                }
            }
            return false;
        }

        IEnumerable<UnityEngine.Object> ObjectsForSubAsset()
        {
            HashSet<Texture2D> textures = new HashSet<Texture2D>();
            foreach (var x in Textures.SelectMany(y => y.GetTexturesForSaveAssets()))
            {
                if (!textures.Contains(x))
                {
                    textures.Add(x);
                }
            }
            foreach (var x in textures) { yield return x; }
            foreach (var x in Materials) { yield return x; }
            foreach (var x in Meshes) { yield return x.Mesh; }
            if (Animation != null) yield return Animation;
        }

        public void SaveAsAsset()
        {
            var path = PrefabPath;
            if (File.Exists(path))
            {
                // clear SubAssets
                foreach (var x in GetSubAssets(path).Where(x => !(x is GameObject) && !(x is Component)))
                {
                    GameObject.DestroyImmediate(x, true);
                }
            }

            // Add SubAsset
            foreach (var o in ObjectsForSubAsset())
            {
                AssetDatabase.AddObjectToAsset(o, path);
            }

            // Create or upate Main Asset
            if (File.Exists(path))
            {
                Debug.LogFormat("replace prefab: {0}", path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                PrefabUtility.ReplacePrefab(Root, prefab, ReplacePrefabOptions.ReplaceNameBased);
            }
            else
            {
                Debug.LogFormat("create prefab: {0}", path);
                PrefabUtility.CreatePrefab(path, Root);
            }

            AssetDatabase.ImportAsset(path);
        }
        #endregion
#endif

        public void Destroy(bool destroySubAssets)
        {
            if (Root != null) GameObject.DestroyImmediate(Root);
            if (destroySubAssets)
            {
#if UNITY_EDITOR
                foreach (var o in ObjectsForSubAsset())
                {
                    UnityEngine.Object.DestroyImmediate(o, true);
                }
#endif
            }
        }
    }
}
