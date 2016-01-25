/*
 * @author Francesco Strada
 */

using Touchables.MultiTouchManager;
using Touchables.TokenEngine;
using Touchables.TokenEngine.TokenTypes;
using UnityEngine;

namespace Touchables
{
    [AddComponentMenu("Touchable/TokensEngine")]
    public class TokensEngine : MonoBehaviour
    {
        public int TokenType;
        public bool MeanSquare;
        public int ComputePixels;
        public float TranslationThr;
        public float RotationThr;
        public bool Target60FPS;
        public bool ContinuousMeanSquare;

        void Awake()
        {
            ClusterManager.Instance.Initialize();

            switch (TokenType)
            {
                case 0:
                    TokenManager.Instance.SetApplicationTokenType(new Token3x3());
                    break;
                case 1:
                    TokenManager.Instance.SetApplicationTokenType(new Token4x4());
                    break;
                case 2:
                    TokenManager.Instance.SetApplicationTokenType(new Token5x5());
                    break;
            }

            TokenManager.Instance.Initialize();
            TokenManager.Instance.SetClassComputeReferenceSystem(MeanSquare);
            TokenManager.Instance.ContinuousMeanSquare = ContinuousMeanSquare;

            if (ComputePixels == 0)
                TokenManager.Instance.SetClassComputeDimensions(true);
            else
                TokenManager.Instance.SetClassComputeDimensions(false);


            TokenManager.Instance.SetTokenUpdateTranslationThr(TranslationThr);
            TokenManager.Instance.SetTokenUpdateRotationThr(RotationThr);

            if (Target60FPS)
                Application.targetFrameRate = 60;

        }

        void Update()
        {
            InputServer.Instance.Update();
            InputManager.UpdateFingersCancelled();
        }

        void OnDestroy()
        {
            ClusterManager.Instance.Disable();
            TokenManager.Instance.Disable();
        }
    }
}
