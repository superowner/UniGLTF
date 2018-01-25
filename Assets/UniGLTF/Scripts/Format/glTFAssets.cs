﻿using System;


namespace UniGLTF
{
    [Serializable]
    public struct glTFAssets
    {
        public string generator;
        public string version;

        public override string ToString()
        {
            return string.Format("GLTF-{0} generated by {1}", version, generator);
        }
    }
}