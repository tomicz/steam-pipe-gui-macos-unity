using System;
using UnityEngine;

namespace Tomicz.Deployer
{
    /// <summary>
    /// One Steam depot that receives part or all of a build.
    /// </summary>
    [Serializable]
    public class Depot
    {
        public string DepotId => _depotId;

        /// <summary>
        /// Files to upload, relative to the build folder. Never empty: an empty value means everything.
        /// </summary>
        public string LocalPath => string.IsNullOrEmpty(_localPath) ? "*" : _localPath;

        [SerializeField] private string _depotId = "";
        [Tooltip("Files to upload to this depot, relative to the build folder. Wildcards allowed. Use * for everything or DLC/* for a subfolder.")]
        [SerializeField] private string _localPath = "*";

        public Depot()
        {
        }

        public Depot(string depotId, string localPath)
        {
            _depotId = depotId;
            _localPath = localPath;
        }
    }
}
