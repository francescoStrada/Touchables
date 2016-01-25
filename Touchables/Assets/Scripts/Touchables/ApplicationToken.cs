/*
 * @author Francesco Strada
 */

using System;
using Touchables.TokenEngine;
using UnityEngine;

namespace Touchables
{
    [AddComponentMenu("Touchable/ApplicationToken")]
    public class ApplicationToken : MonoBehaviour
    {
        public int TokenClass;
        public UnityEngine.Object Target;

        public MonoBehaviour[] targetComponents;
        public int selectedComponent;

        private ITokenEvents TokenFunction;

        private event EventHandler<ApplicationTokenEventArgs> ApplicationTokenOnScreen;

        public void OnTokenPlacedOnScreen(object sender, ApplicationTokenEventArgs e)
        {
            TokenFunction.OnTokenPlacedOnScreen(sender, e);
        }

        public void OnTokenRemovedFromScreen(object sender, ApplicationTokenEventArgs e)
        {
            TokenFunction.OnTokenRemovedFromScreen(sender, e);
        }

        public void OnTokenUpdated(object sender, ApplicationTokenEventArgs e)
        {
            TokenFunction.OnTokenUpdated(sender, e);
        }

        void OnEnable()
        {
            TokenFunction = targetComponents[selectedComponent] as ITokenEvents;
            if (TokenFunction != null)
            {
                TokenManager.Instance.TokenPlacedOnScreen += OnTokenPlacedOnScreen;
                TokenManager.Instance.ScreenTokenUpdated += OnTokenUpdated;
                TokenManager.Instance.TokenRemovedFromScreen += OnTokenRemovedFromScreen;
            }
            
        }

        void OnDisable()
        {
            if (TokenFunction != null)
            {
                TokenManager.Instance.TokenPlacedOnScreen -= OnTokenPlacedOnScreen;
                TokenManager.Instance.ScreenTokenUpdated -= OnTokenUpdated;
                TokenManager.Instance.TokenRemovedFromScreen -= OnTokenRemovedFromScreen;
            }
                
        }
    }
}
