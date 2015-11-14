/*
 * @author Francesco Strada
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Touchables.Utils;

namespace Touchables.MultiTouchManager
{
    internal sealed class Cluster
    {
        #region Fields

        private Dictionary<int, TouchInput> _points = new Dictionary<int, TouchInput>();
        private HashSet<int> _pointsIds = new HashSet<int>();

        private String _hashId;
        private Vector2 _centroid;
        private ClusterState _state;

        private string _cancelledHash;
        private HashSet<int> _cancelledPointsIds = new HashSet<int>();

        #endregion

        #region Constructors

        public Cluster(TouchInput point)
        {
            this.AddPoint(point.Id);
            this._hashId = "#" + point.Id;
            this._centroid = point.Position;
            _state = ClusterState.Invalid;
        }
        public Cluster(List<TouchInput> touchPoints)
        {
            foreach (TouchInput t in touchPoints)
            {
                _pointsIds.Add(t.Id);
                _points.Add(t.Id, t);
            }
            UpdateCentroid();
            this._hashId = ClusterUtils.GetPointsHash(_pointsIds.ToArray<int>());
            if (_pointsIds.Count == 4)
                this._state = ClusterState.Unidentified;
            else
                this._state = ClusterState.Invalid;
        }

        public Cluster() { }

        #endregion

        #region Properties

        public String Hash
        {
            get
            {
                return _hashId;
            }
        }

        public Vector2 Centroid
        {
            get
            {
                return _centroid;
            }
        }

        public String CancelledClusterHash
        {
            get
            {
                return _cancelledHash;
            }
        }

        public HashSet<int> CancelledPointIds
        {
            get
            {
                return _cancelledPointsIds;
            }
        }

        public Dictionary<int, TouchInput> Points
        {
            get
            {
                return _points;
            }
        }

        public HashSet<int> PointsIds
        {
            get
            {
                return _pointsIds;
            }
        }

        public ClusterState State
        {
            get
            {
                return _state;
            }
        }

        #endregion

        #region Public Methods

        public Cluster AddPoint(int touchId)
        {
            TouchInput touch = new TouchInput(touchId, InternalTouches.List[touchId].Position, InternalTouches.List[touchId].State);
            _pointsIds.Add(touchId);
            _points.Add(touchId, touch);
            UpdateCentroid();
            this._hashId = ClusterUtils.GetPointsHash(_pointsIds.ToArray<int>());

            if (_pointsIds.Count == 4)
                this._state = ClusterState.Unidentified;

            else if (_pointsIds.Count > 4)
                this._state = ClusterState.Invalid;

            return this;
        }

        public Cluster[] UpdatePoint(int touchId)
        {
            Cluster newCluster;
            if (Vector2.Distance(this.Centroid, InternalTouches.List[touchId].Position) < ClusterManager.Instance.ClusterDistThreshold)
            {
                //Point still in clustrer
                _points[touchId] = new TouchInput(touchId, InternalTouches.List[touchId].Position, TouchState.Moved);
                newCluster = null;

                if (State == ClusterState.Identidied)
                    _state = ClusterState.Updated;
            }
            else
            {
                //Point has moved out of the cluster
                //Handle current Cluster

                //If it was just one point then we must cancel the cluster!!!!!!
                //       if (_pointsIds.Count != 1)
                //       {
                _pointsIds.Remove(touchId);
                _points.Remove(touchId);

                if (_state == ClusterState.Identidied || _state == ClusterState.Updated)
                {
                    _state = ClusterState.Cancelled;
                    _cancelledHash = this._hashId;
                    _cancelledPointsIds.Add(touchId);
                }

                else if (State == ClusterState.Cancelled)
                    _cancelledPointsIds.Add(touchId);

                else if (_pointsIds.Count == 4)
                    _state = ClusterState.Unidentified;

                else
                    _state = ClusterState.Invalid;

                //Update new Hash
                this._hashId = ClusterUtils.GetPointsHash(_pointsIds.ToArray<int>());
                //       }
                //        else


                newCluster = new Cluster(InternalTouches.List[touchId]);

            }

            UpdateCentroid();

            return new Cluster[] { this, newCluster };

        }

        public Cluster RemovePoint(int touchId)
        {
            _pointsIds.Remove(touchId);
            _points.Remove(touchId);
            UpdateCentroid();

            if (State == ClusterState.Identidied || State == ClusterState.Updated)
            {
                _state = ClusterState.Cancelled;
                _cancelledHash = this._hashId;
                _cancelledPointsIds.Add(touchId);
            }
            else if (State == ClusterState.Cancelled)
                _cancelledPointsIds.Add(touchId);

            else if (_pointsIds.Count == 4)
                _state = ClusterState.Unidentified;
            else
                _state = ClusterState.Invalid;

            this._hashId = ClusterUtils.GetPointsHash(_pointsIds.ToArray<int>());

            return this;
        }

        public void SetState(ClusterState newState)
        {
            this._state = newState;
        }

        public void SetCancelledClusterHash(string hash)
        {
            this._cancelledHash = hash;
        }

        public void SetCancelledPointIds(HashSet<int> cancelledIds)
        {
            this._cancelledPointsIds = cancelledIds;
        }

        #endregion

        #region Private Methods

        private void UpdateCentroid()
        {
            float xTmp = 0f;
            float yTmp = 0f;
            foreach (KeyValuePair<int, TouchInput> entry in _points)
            {
                xTmp += entry.Value.Position.x;
                yTmp += entry.Value.Position.y;
            }

            if (_points.Count != 0)
            {
                _centroid.x = xTmp / _points.Count;
                _centroid.y = yTmp / _points.Count;
            }

        }
        #endregion        
    }

    public enum ClusterState
    {
        Unidentified,

        Identidied,

        Updated,

        Cancelled,

        Invalid
    }
}
