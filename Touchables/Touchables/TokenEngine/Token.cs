/*
 * @author Francesco Strada
 */

using UnityEngine;

namespace Touchables.TokenEngine
{
    public class Token : IToken
    {
        #region Properties

        private int _id;
        private int? _class;
        private Vector2 _position;
        private Vector2 _deltaPosition;
        private float _angle;
        private float _deltaAngle;
        
        #endregion
        
        #region Public Fields 
        
        /// <inheritdoc />
        public int Id { get { return _id; } }

        /// <inheritdoc />
        public int? Class { get { return _class; } }

        /// <inheritdoc />
        public Vector2 Position { get { return _position; } }

        /// <inheritdoc />
        public Vector2 DeltaPosition { get { return _deltaPosition; } }

        /// <inheritdoc />
        public float Angle { get { return _angle; } }

        /// <inheritdoc />
        public float DeltaAngle { get { return _deltaAngle; } }

        #endregion

        #region Constructor

        internal Token(InternalToken internalToken)
        {
            this._id = internalToken.Id;
            this._class = internalToken.Class;
            this._position = internalToken.Position;
            this._angle = internalToken.Angle;
            this._deltaAngle = internalToken.DeltaAngle;
            this._deltaPosition = internalToken.DeltaPosition;

        }

        #endregion

        #region

        internal void UpdateToken(InternalToken internalToken)
        {
            this._position = internalToken.Position;
            this._angle = internalToken.Angle;
            this._deltaAngle = internalToken.DeltaAngle;
            this._deltaPosition = internalToken.DeltaPosition;

        }

        #endregion
    }
}
