/*
 * @author Francesco Strada
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Touchables.TokenEngine.TokenTypes;
using Touchables.MultiTouchManager;
using UnityEngine;
using Touchables.Utils;

namespace Touchables.TokenEngine
{
    internal sealed class TokenManager
    {
        #region Fields

        private static readonly TokenManager _instance = new TokenManager();

        private Dictionary<string, InternalToken> tokens = new Dictionary<string, InternalToken>();
        private HashSet<int> tokenIds = new HashSet<int>();

        private List<InternalToken> succesfullyIdentifiedTokens = new List<InternalToken>(2);
        private List<InternalToken> failedIdentifiedTokens = new List<InternalToken>(2);
        private List<InternalToken> identifiedTokensMoved = new List<InternalToken>(2);
        private List<InternalToken> identifiedTokensCancelled = new List<InternalToken>(2);

        internal static TokenType CurrentTokenType;

        readonly object TokenCallBackLock = new object();

        private ClassComputeReferenceSystem ClassComputeRefSystem;
        private ClassComputeDimension ClassComputeDimension;

        private float TokenUpdateTranslationThreshold;
        private float TokenUpdateRotationThreshold;

        public bool ContinuousMeanSquare;

        private TokenStatistics tokenStatistics;

        #endregion

        #region Events
        internal event EventHandler<InternalTokenIdentifiedEventArgs> TokenIdentifiedEvent;
        internal event EventHandler<InternalTokenCancelledEventArgs> TokenCancelledEvent;

        internal event EventHandler<ApplicationTokenEventArgs> TokenPlacedOnScreen;
        internal event EventHandler<ApplicationTokenEventArgs> ScreenTokenUpdated;
        internal event EventHandler<ApplicationTokenEventArgs> TokenRemovedFromScreen;
        #endregion

        #region Public Properties

        public static TokenManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public List<InternalToken> Tokens
        {
            get
            {
                return tokens.Values.ToList();
            }
        }

        #endregion

        #region Constructor

        private TokenManager() { }

        #endregion

        #region Public Methods

        public TokenManager Initialize()
        {
            //ClusterManager.Instance.ClustersToIdentifyEvent += OnClustersToIdentify;
            //ClusterManager.Instance.ClustersMovedEvent += OnClustersMoved;
            //ClusterManager.Instance.ClustersCancelledEvent += OnClustersCancelled;

            ClusterManager.Instance.ClustersUpdateEvent += OnClustersUpdated;

            ClusterManager.Instance.SetClusterDistThreshold(CurrentTokenType.TokenDiagonalPX);

            tokenStatistics = TokenStatistics.Instance;

            return _instance;
        }

        public TokenManager Disable()
        {
            //ClusterManager.Instance.ClustersToIdentifyEvent -= OnClustersToIdentify;
            //ClusterManager.Instance.ClustersMovedEvent -= OnClustersMoved;
            //ClusterManager.Instance.ClustersCancelledEvent -= OnClustersCancelled;

            ClusterManager.Instance.ClustersUpdateEvent -= OnClustersUpdated;

            tokenStatistics.ResetMetrics();

            return _instance;

        }

        public void SetApplicationTokenType(TokenType t)
        {
            CurrentTokenType = t;
        }

        public void SetClassComputeReferenceSystem(bool SetMeanSquare)
        {
            if (SetMeanSquare)
                ClassComputeRefSystem = ClassComputeReferenceSystem.MeanSqure;
            else
                ClassComputeRefSystem = ClassComputeReferenceSystem.Regular;
        }

        public void SetClassComputeDimensions(bool SetPixels)
        {
            if (SetPixels)
                ClassComputeDimension = ClassComputeDimension.Pixels;
            else
                ClassComputeDimension = ClassComputeDimension.Centimeters;
        }

        public void SetTokenUpdateTranslationThr(float value)
        {
            TokenUpdateTranslationThreshold = value;
        }

        public void SetTokenUpdateRotationThr(float value)
        {
            TokenUpdateRotationThreshold = value;
        }

        #endregion

        #region Private Methods

        private int GetFirstAvailableTokenId()
        {
            int defValue = 0;
            for (int i = 0; i < int.MaxValue; i++)
            {
                if (!tokenIds.Contains(i))
                    return i;
            }
            return defValue;
        }

        private bool UpdateGreaterThanThreshold(InternalToken token)
        {
            if (Math.Abs(token.DeltaPosition.x) > TokenUpdateTranslationThreshold || Math.Abs(token.DeltaPosition.y) > TokenUpdateTranslationThreshold ||
               Math.Abs(token.DeltaAngle) > TokenUpdateRotationThreshold)
                return true;
            else
                return false;
        }

        private void IdentifyCluster(Cluster cluster)
        {
            //TODO this function requires updates
            Profiler.BeginSample("---TokenEngine : Token Identification");
            InternalToken token = TokenIdentification.Instance.IdentifyCluster(cluster);
            Profiler.EndSample();
            if (token != null)
            {
                //Statistics
                tokenStatistics.TokenIdentification(true);

                //Calculate TokenClass
                Profiler.BeginSample("---TokenEngine: Class LUT");
                token.ComputeTokenClass(ClassComputeRefSystem, ClassComputeDimension);
                Profiler.EndSample();

                tokenStatistics.TokenClassRecognition(token.Class);

                //Cluster Identification was succesfull

                //Set Token ID
                token.SetTokenId(GetFirstAvailableTokenId());
                tokenIds.Add(token.Id);

                //Add Token to internal List
                tokens.Add(token.HashId, token);

                //Add Token To Global List
                InputManager.AddToken(new Token(token, ContinuousMeanSquare));

                //Add Token TO local buffer
                succesfullyIdentifiedTokens.Add(token);
            }
            else
            {
                tokenStatistics.TokenIdentification(false);

                //Add token to local buffer
                failedIdentifiedTokens.Add(token);
            }

        }

        private void UpdateClusterMoved(Cluster cluster)
        {
            //Update internally the token
            InternalToken internalToken;
            if (tokens.TryGetValue(cluster.Hash, out internalToken))
            {
                internalToken.Update(cluster);
                tokens.Remove(cluster.Hash);
                tokens.Add(internalToken.HashId, internalToken);

                //Update Global Token
                InputManager.GetToken(internalToken.Id).UpdateToken(internalToken, ContinuousMeanSquare);

                //Add token to local buffer
                identifiedTokensMoved.Add(internalToken);
            }
        }

        private void CancelCluster(Cluster cluster)
        {
            //Cancel cluster according to point
            //Here is very delicate because it must be considered also the possibility of not removing the token untill not all points have been removed
            InternalToken token;
            if (tokens.TryGetValue(cluster.CancelledClusterHash, out token))
            {
                tokenIds.Remove(token.Id);
                InputManager.RemoveToken(token.Id);

                tokens.Remove(cluster.CancelledClusterHash);

                //Add token to local buffer
                identifiedTokensCancelled.Add(token);
            }
        }

        private void DispatchTokenEventsFromBuffers()
        {
            foreach(InternalToken token in identifiedTokensCancelled)
            {
                //Launch CallBack to CM
                OnTokenCancelledEvent(new InternalTokenCancelledEventArgs(token.HashId));

                //Lauch Application Token Cancelled
                OnTokenRemovedFromScreenEvent(new ApplicationTokenEventArgs(new Token(token, ContinuousMeanSquare)));
            }

            foreach(InternalToken token in identifiedTokensMoved)
            {
                //Check deltas in order to fire or not Events
                if (UpdateGreaterThanThreshold(token))
                    OnScreenTokenUpdatedEvent(new ApplicationTokenEventArgs(new Token(token, ContinuousMeanSquare)));
            }

            foreach(InternalToken token in failedIdentifiedTokens)
            {
                //Cluser Identification failed, need to report back to Cluster Manager
                OnTokenIdentifiedEvent(new InternalTokenIdentifiedEventArgs(token.HashId, false));
            }

            foreach(InternalToken token in succesfullyIdentifiedTokens)
            {
                //Fire event token identified
                OnTokenPlacedOnScreenEvent(new ApplicationTokenEventArgs(new Token(token, ContinuousMeanSquare)));

                //Notify ClusterManager cluster has been identified
                OnTokenIdentifiedEvent(new InternalTokenIdentifiedEventArgs(token.HashId, true));
            }

            //Reset all buffers
            identifiedTokensCancelled.Clear();
            identifiedTokensMoved.Clear();
            failedIdentifiedTokens.Clear();
            succesfullyIdentifiedTokens.Clear();

        }
        #endregion

        #region Event Handlers

        //private void OnClustersToIdentify(object sender, ClusterUpdateEventArgs e)
        //{
        //    foreach (Cluster cluster in e.GetClusters())
        //    {
        //        IdentifyCluster(cluster);
        //    }
        //}

        //private void OnClustersMoved(object sender, ClusterUpdateEventArgs e)
        //{
        //    foreach (Cluster cluster in e.GetClusters())
        //    {
        //        UpdateClusterMoved(cluster);
        //    }
        //}

        //private void OnClustersCancelled(object sender, ClusterUpdateEventArgs e)
        //{
        //    foreach (Cluster cluster in e.GetClusters())
        //    {
        //        CancelCluster(cluster);

        //    }
        //}

        private void OnClustersUpdated(object sender, ClustersUpdateEventArgs e)
        {
            foreach(Cluster c in e.ClustersToIdentify)
            {
                IdentifyCluster(c);
            }

            foreach(Cluster c in e.ClustersIdentifiedMoved)
            {
                UpdateClusterMoved(c);
            }

            foreach(Cluster c in e.ClustersIdentifiedCancelled)
            {
                CancelCluster(c);
            }

            DispatchTokenEventsFromBuffers();
        }

        #endregion

        #region Event Launchers

        private void OnTokenIdentifiedEvent(InternalTokenIdentifiedEventArgs e)
        {
            EventHandler<InternalTokenIdentifiedEventArgs> handler;

            lock (TokenCallBackLock)
            {
                handler = TokenIdentifiedEvent;
            }
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnTokenCancelledEvent(InternalTokenCancelledEventArgs e)
        {
            EventHandler<InternalTokenCancelledEventArgs> handler;
            lock (TokenCallBackLock)
            {
                handler = TokenCancelledEvent;
            }

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnTokenPlacedOnScreenEvent(ApplicationTokenEventArgs e)
        {
            EventHandler<ApplicationTokenEventArgs> handler;
            List<Delegate> subscribers = new List<Delegate>();
            int? invokerTokenClass = null;

            lock (TokenCallBackLock)
            {
                handler = TokenPlacedOnScreen;
            }

            if (handler != null)
            {
                subscribers = handler.GetInvocationList().ToList();
                invokerTokenClass = e.Token.Class;
                DispatchEventToSubscribers(subscribers, invokerTokenClass, e);
            }


        }

        private void OnScreenTokenUpdatedEvent(ApplicationTokenEventArgs e)
        {
            EventHandler<ApplicationTokenEventArgs> handler;
            List<Delegate> subscribers = new List<Delegate>();
            int? invokerTokenClass = null;

            lock (TokenCallBackLock)
            {
                handler = ScreenTokenUpdated;
            }

            if (handler != null)
            {
                subscribers = handler.GetInvocationList().ToList();
                invokerTokenClass = e.Token.Class;
                DispatchEventToSubscribers(subscribers, invokerTokenClass, e);
            }
        }

        private void OnTokenRemovedFromScreenEvent(ApplicationTokenEventArgs e)
        {
            EventHandler<ApplicationTokenEventArgs> handler;
            List<Delegate> subscribers = new List<Delegate>();
            int? invokerTokenClass = null;

            lock (TokenCallBackLock)
            {
                handler = TokenRemovedFromScreen;
            }

            if (handler != null)
            {
                subscribers = handler.GetInvocationList().ToList();
                invokerTokenClass = e.Token.Class;
                DispatchEventToSubscribers(subscribers, invokerTokenClass, e);
            }
        }

        private void DispatchEventToSubscribers(List<Delegate> subscribers, int? invokerTokenClass, ApplicationTokenEventArgs e)
        {
            foreach (Delegate subscriber in subscribers)
            {
                ApplicationToken applicationToken = subscriber.Target as ApplicationToken;
                int subscriberTokenClass = applicationToken.TokenClass;

                if (invokerTokenClass == subscriberTokenClass)
                {
                    EventHandler<ApplicationTokenEventArgs> subscriberHandler = subscriber as EventHandler<ApplicationTokenEventArgs>;
                    subscriberHandler(this, e);
                }
            }
        }
        #endregion
    }

    internal class InternalTokenIdentifiedEventArgs : EventArgs
    {
        private string _tokenHashId;
        private bool _success;

        internal string TokenHashId { get { return _tokenHashId; } }
        internal bool Success { get { return _success; } }

        internal InternalTokenIdentifiedEventArgs(string tokenHashId, bool success)
        {
            this._tokenHashId = tokenHashId;
            this._success = success;
        }
    }

    internal class InternalTokenCancelledEventArgs : EventArgs
    {
        private string _tokenHashId;

        internal string TokenHashId { get { return _tokenHashId; } }

        internal InternalTokenCancelledEventArgs(string tokenHashId)
        {
            this._tokenHashId = tokenHashId;
        }
    }

    public class ApplicationTokenEventArgs : EventArgs
    {
        private Token _token;
        public Token Token { get { return _token; } }

        internal ApplicationTokenEventArgs(Token token)
        {
            this._token = token;
        }
    }
}
