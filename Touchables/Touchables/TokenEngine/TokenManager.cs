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

        internal static TokenType CurrentTokenType;

        readonly object TokenCallBackLock = new object();

        private ClassComputeReferenceSystem ClassComputeRefSystem;
        private ClassComputeDimension ClassComputeDimension;

        private float TokenUpdateTranslationThreshold;
        private float TokenUpdateRotationThreshold;

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
            ClusterManager.Instance.ClustersToIdentifyEvent += OnClustersToIdentify;
            ClusterManager.Instance.ClustersMovedEvent += OnClustersMoved;
            ClusterManager.Instance.ClustersCancelledEvent += OnClustersCancelled;

            ClusterManager.Instance.SetClusterDistThreshold(CurrentTokenType.TokenDiagonalPX);

            tokenStatistics = TokenStatistics.Instance;

            return _instance;
        }

        public TokenManager Disable()
        {
            ClusterManager.Instance.ClustersToIdentifyEvent -= OnClustersToIdentify;
            ClusterManager.Instance.ClustersMovedEvent -= OnClustersMoved;
            ClusterManager.Instance.ClustersCancelledEvent -= OnClustersCancelled;

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

        #endregion

        #region Event Handlers

        private void OnClustersToIdentify(object sender, ClusterUpdateEventArgs e)
        {
            foreach (Cluster cluster in e.GetClusters())
            {
                //TODO this function requires updates
                InternalToken token = TokenIdentification.Instance.IdentifyCluster(cluster);

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
                    InputManager.AddToken(new Token(token));

                    //Notify ClusterManager cluster has been identified
                    OnTokenIdentifiedEvent(new InternalTokenIdentifiedEventArgs(token.HashId, true));

                    //Fire event token identified
                    OnTokenPlacedOnScreenEvent(new ApplicationTokenEventArgs(new Token(token)));

                }
                else
                {
                    tokenStatistics.TokenIdentification(false);
                    //Cluser Identification failed, need to report back to Cluster Manager
                    OnTokenIdentifiedEvent(new InternalTokenIdentifiedEventArgs(cluster.Hash, false));

                }
            }
        }

        private void OnClustersMoved(object sender, ClusterUpdateEventArgs e)
        {
            foreach (Cluster cluster in e.GetClusters())
            {
                //Update internally the token
                InternalToken internalToken;
                if (tokens.TryGetValue(cluster.Hash, out internalToken))
                {
                    internalToken.Update(cluster);
                    tokens.Remove(cluster.Hash);
                    tokens.Add(internalToken.HashId, internalToken);

                    //Update Global Token
                    InputManager.GetToken(internalToken.Id).UpdateToken(internalToken);

                    //Check deltas in order to fire or not Events
                    if (UpdateGreaterThanThreshold(internalToken))
                        OnScreenTokenUpdatedEvent(new ApplicationTokenEventArgs(new Token(internalToken)));

                }
            }
        }

        private void OnClustersCancelled(object sender, ClusterUpdateEventArgs e)
        {
            foreach (Cluster cluster in e.GetClusters())
            {
                //Cancel cluster according to point
                //Here is very delicate because it must be considered also the possibility of not removing the token untill not all points have been removed
                InternalToken token;
                if (tokens.TryGetValue(cluster.CancelledClusterHash, out token))
                {
                    tokenIds.Remove(token.Id);
                    InputManager.RemoveToken(token.Id);

                    tokens.Remove(cluster.CancelledClusterHash);

                    //Launch CallBack to CM
                    OnTokenCancelledEvent(new InternalTokenCancelledEventArgs(cluster.Hash));

                    //Lauch Application Token Cancelled
                    OnTokenRemovedFromScreenEvent(new ApplicationTokenEventArgs(new Token(token)));
                }

            }
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
                IApplicationToken applicationToken = subscriber.Target as IApplicationToken;
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
