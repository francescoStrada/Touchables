using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Touchables.MultiTouchManager
{
    internal sealed class ClusterManager
    {
        #region Events

        public event EventHandler<ClusterUpdateEventArgs> ClustersToIdentifyEvent;
        public event EventHandler<ClusterUpdateEventArgs> ClustersMovedEvent;
        public event EventHandler<ClusterUpdateEventArgs> ClustersCancelledEvent;

        #endregion

        #region Private Fields
        private static readonly ClusterManager _instance = new ClusterManager();
        private Dictionary<String, Cluster> clusters = new Dictionary<String, Cluster>();

        private List<Cluster> ClustersToIdentify = new List<Cluster>();
        private List<Cluster> IdentifiedClustersMoved = new List<Cluster>();
        private Dictionary<string, Cluster> IdentifiedClustersCancelled = new Dictionary<string, Cluster>();

        readonly object ClusterUpdateLock = new object();

        #endregion

        #region Public Properties
        public static ClusterManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public List<Cluster> Clusters
        {
            get
            {
                return clusters.Values.ToList();
            }
        }

        public float ClusterDistThreshold { get; private set; }
        #endregion 

        private ClusterManager() { }

        #region Public Methods
        public ClusterManager Initialize()
        {
            InputServer.Instance.InputUpdated += OnInputsUpdateEventHandler;
            TokenManager.Instance.TokenIdentifiedEvent += OnTokenIdentified;
            //TokenManager.Instance.TokenCancelledEvent += OnTokenCancelled;
            return _instance;
        }

        public void Disable()
        {
            InputServer.Instance.InputUpdated -= OnInputsUpdateEventHandler;
            TokenManager.Instance.TokenIdentifiedEvent -= OnTokenIdentified;
        }

        public void SetClusterDistThreshold(float threshold)
        {
            this.ClusterDistThreshold = threshold;
        }
        #endregion

        #region Private Methods
        private String GetClusterIdFromTouchPoint(int touchPointId)
        {
            foreach (KeyValuePair<String, Cluster> entry in clusters)
            {
                if (entry.Value.PointsIds.Contains(touchPointId))
                    return entry.Key;
            }
            return null;
        }

        private void UpdateClusters()
        {
            float minDist = this.ClusterDistThreshold;
            float dist;
            bool minFound = false;
            List<Cluster> clustersList = this.clusters.Values.ToList();

            int mergeCluster1Index = 0;
            int mergeCluster2Index = 0;
            Cluster r1 = new Cluster();
            Cluster r2 = new Cluster();

            if (clusters.Count > 1)
            {
                while (minDist <= this.ClusterDistThreshold)
                {
                    for (int i = 0; i < clustersList.Count; i++)
                    {
                        for (int j = i + 1; j < clustersList.Count; j++)
                        {
                            Cluster c1 = clustersList.ElementAt(i);
                            Cluster c2 = clustersList.ElementAt(j);

                            dist = Vector2.Distance(c1.Centroid, c2.Centroid);

                            if (dist < minDist)
                            {
                                minDist = dist;
                                mergeCluster1Index = i;
                                mergeCluster2Index = j;
                                r1 = clustersList[i];
                                r2 = clustersList[j];
                                minFound = true;
                            }
                        }
                    }
                    if (minFound)
                    {
                        minFound = false;
                        Cluster mergedCluster = MergeClusters(clustersList.ElementAt(mergeCluster1Index), clustersList.ElementAt(mergeCluster2Index));
                        clustersList.Remove(r1);
                        clustersList.Remove(r2);
                        clustersList.Add(mergedCluster);
                    }
                    else
                        minDist *= 2;
                }

                clusters = clustersList.ToDictionary(v => v.Hash, v => v);
            }
        }

        private Cluster MergeClusters(Cluster c1, Cluster c2)
        {
            if (c1.State == ClusterState.Updated || c1.State == ClusterState.Identidied)
            {
                c1.SetCancelledClusterHash(c1.Hash);
                c1.SetCancelledPointIds(c1.PointsIds);
                c1.SetState(ClusterState.Cancelled);
                IdentifiedClustersCancelled.Add(c1.Hash, c1);
            }
            if (c2.State == ClusterState.Updated || c2.State == ClusterState.Identidied)
            {
                c2.SetCancelledClusterHash(c2.Hash);
                c2.SetCancelledPointIds(c2.PointsIds);
                c2.SetState(ClusterState.Cancelled);
                IdentifiedClustersCancelled.Add(c2.Hash, c2);

            }

            List<TouchInput> allPoints = c1.Points.Values.ToList();
            allPoints.AddRange(c2.Points.Values.ToList());

            return new Cluster(allPoints);
        }

        private void CheckClustersUpdated()
        {
            foreach (KeyValuePair<String, Cluster> entry in clusters)
            {
                Cluster cluster = entry.Value;
                switch (cluster.State)
                {
                    case ClusterState.Unidentified:
                        {
                            //Cluster has reached for points and needs to be sent to identifier for check
                            ClustersToIdentify.Add(cluster);
                            break;
                        }
                    case ClusterState.Updated:
                        {
                            //Identified cluster has moved
                            IdentifiedClustersMoved.Add(cluster);
                            break;
                        }
                }
            }
        }

        private void OnClusterToIdentify(ClusterUpdateEventArgs e)
        {
            EventHandler<ClusterUpdateEventArgs> handler;

            lock (ClusterUpdateLock)
            {
                handler = ClustersToIdentifyEvent;
            }

            if (handler != null)
            {
                handler(this, e);
            }

        }

        private void OnClustersMoved(ClusterUpdateEventArgs e)
        {
            EventHandler<ClusterUpdateEventArgs> handler;

            lock (ClusterUpdateLock)
            {
                handler = ClustersMovedEvent;
            }

            if (handler != null)
            {
                handler(this, e);
            }

        }

        private void OnClustersCancelled(ClusterUpdateEventArgs e)
        {
            EventHandler<ClusterUpdateEventArgs> handler;

            lock (ClusterUpdateLock)
            {
                handler = ClustersCancelledEvent;
            }

            if (handler != null)
            {
                handler(this, e);
            }

        }

        private void ResetClustersBuffers()
        {
            ClustersToIdentify.Clear();
            IdentifiedClustersMoved.Clear();
            IdentifiedClustersCancelled.Clear();

        }
        #endregion

        #region Event Handlers
        private void OnInputsUpdateEventHandler(object sender, InputUpdateEventArgs e)
        {

            String clusterHash;

            ResetClustersBuffers();

            foreach (int touchId in InternalTouches.CancelledTouchBuffer)
            {
                clusterHash = GetClusterIdFromTouchPoint(touchId);
                if (clusterHash != null)
                {
                    //Is a cluster with more than one point
                    if (clusters[clusterHash].PointsIds.Count > 1)
                    {
                        Cluster updatedCluster = clusters[clusterHash].RemovePoint(touchId);
                        //Update Current state Clusters
                        clusters.Remove(clusterHash);
                        clusters.Add(updatedCluster.Hash, updatedCluster);

                        //If State is Cancelled update CancelledCluster Buffer
                        if (updatedCluster.State == ClusterState.Cancelled)
                        {
                            IdentifiedClustersCancelled.Remove(updatedCluster.CancelledClusterHash);
                            IdentifiedClustersCancelled.Add(updatedCluster.CancelledClusterHash, updatedCluster);
                        }
                    }
                    //Is a cluster with only one point
                    else
                    {
                        //Update CancelledClusterBuffer
                        Cluster cluster = clusters[clusterHash].RemovePoint(touchId);
                        if (cluster.State == ClusterState.Cancelled)
                        {
                            IdentifiedClustersCancelled.Remove(cluster.CancelledClusterHash);
                            IdentifiedClustersCancelled.Add(cluster.CancelledClusterHash, cluster);
                        }

                        //Remove cluster from current Clusters
                        clusters.Remove(clusterHash);
                    }


                }
                //Remove touch from fingers touch list

            }

            foreach (int touchId in InternalTouches.MovedTouchBuffer)
            {
                clusterHash = GetClusterIdFromTouchPoint(touchId);
                if (clusterHash != null)
                {
                    Cluster[] updatedCluster = clusters[clusterHash].UpdatePoint(touchId);

                    clusters.Remove(clusterHash);
                    if (updatedCluster[0].Points.Count != 0)
                    {
                        clusters.Add(updatedCluster[0].Hash, updatedCluster[0]);
                        if (updatedCluster[0].State == ClusterState.Cancelled)
                        {
                            IdentifiedClustersCancelled.Remove(updatedCluster[0].CancelledClusterHash);
                            IdentifiedClustersCancelled.Add(updatedCluster[0].CancelledClusterHash, updatedCluster[0]);
                        }
                    }

                    //Its the case where a previous cluster gets separeted into two
                    if (updatedCluster[1] != null)
                        clusters.Add(updatedCluster[1].Hash, updatedCluster[1]);

                }
                else
                {
                    Cluster c = new Cluster(InternalTouches.List[touchId]);
                    clusters.Add(c.Hash, c);
                }
            }


            foreach (int touchId in InternalTouches.BaganTouhBuffer)
            {
                Cluster cl = new Cluster(InternalTouches.List[touchId]);
                clusters.Add(cl.Hash, cl);

            }

            if (InternalTouches.CancelledTouchBuffer.Count > 0 || InternalTouches.MovedTouchBuffer.Count > 0 || InternalTouches.BaganTouhBuffer.Count > 0)
            {
                UpdateClusters();

                CheckClustersUpdated();

                if (IdentifiedClustersCancelled.Count > 0)
                    OnClustersCancelled(new ClusterUpdateEventArgs("Moved cluster request", IdentifiedClustersCancelled.Values.ToList()));

                if (IdentifiedClustersMoved.Count > 0)
                    OnClustersMoved(new ClusterUpdateEventArgs("Moved cluster request", IdentifiedClustersMoved));

                if (ClustersToIdentify.Count > 0)
                    OnClusterToIdentify(new ClusterUpdateEventArgs("Identification request", ClustersToIdentify));


                //Get points which are touches and not markers
                InputManager.SetFingersCancelled(InternalTouches.CancelledTouchBuffer.ToArray());

                foreach (Cluster c in clusters.Values)
                {
                    if (c.State == ClusterState.Invalid || c.State == ClusterState.Cancelled)
                    {
                        //This cluster is only made of finger touch points
                        foreach (TouchInput touch in c.Points.Values)
                        {
                            InputManager.AddFingerTouch(touch);
                        }

                    }

                }
            }

        }

        private void OnTokenIdentified(object sender, InternalTokenIdentifiedEventArgs e)
        {
            if (e.Success)
            {
                clusters[e.TokenHashId].SetState(ClusterState.Identidied);
                InputManager.SetFingersCancelled(clusters[e.TokenHashId].PointsIds.ToArray());
            }
            else
            {
                clusters[e.TokenHashId].SetState(ClusterState.Invalid);
            }
        }

        private void OnTokenCancelled(object sender, InternalTokenCancelledEventArgs e)
        {
            //clusters[e.TokenHashId].SetState(ClusterState.Invalid);

            //Add these to FingerTouches
            //Cluster c = clusters[e.TokenHashId];
            //foreach (KeyValuePair<int, TouchInput> record in c.Points)
            //{
            //    InputManager.AddFingerTouch(record.Value);
            //}
        }
        #endregion
    }

    //TODO eventual customized event args for cancelled cluster in which put also the index of the touch point cancelled

    /// <summary>
    /// 
    /// </summary>
    internal class ClusterUpdateEventArgs : EventArgs
    {
        internal string EventMsg { get { return _eventMsg; } }

        private List<Cluster> _updatedClusters = new List<Cluster>();
        private string _eventMsg;
        internal ClusterUpdateEventArgs(string msg, List<Cluster> clusters)
        {
            this._eventMsg = msg;
            this._updatedClusters = clusters;
        }

        internal List<Cluster> GetClusters()
        {
            return _updatedClusters;
        }
    }
}
