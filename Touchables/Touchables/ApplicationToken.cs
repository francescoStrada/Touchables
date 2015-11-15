/*
 * @author Francesco Strada
 */

using System;
using Touchables.TokenEngine;
using UnityEngine;

namespace Touchables
{
    [AddComponentMenu("Touchable/ApplicationToken")]
    public class ApplicationToken : MonoBehaviour, ITokenEvents
    {
        public int TokenClass;
        public Component Functions;

        //private IApplicationToken ApplicationFunctions;

        private event EventHandler<ApplicationTokenEventArgs> ApplicationTokenOnScreen;

        public void OnTokenPlacedOnScreen(object sender, ApplicationTokenEventArgs e)
        {
            //ApplicationFunctions.OnTokenPlacedOnScreen(sender, e);
        }

        public void OnTokenRemovedFromScreen(object sender, ApplicationTokenEventArgs e)
        {
            //ApplicationFunctions.OnTokenRemovedFromScreen(sender, e);
        }

        public void OnTokenUpdated(object sender, ApplicationTokenEventArgs e)
        {
            //ApplicationFunctions.OnTokenUpdated(sender, e);
        }

        void OnEnable()
        {
            TokenManager.Instance.TokenPlacedOnScreen += OnTokenPlacedOnScreen;
            TokenManager.Instance.ScreenTokenUpdated += OnTokenUpdated;
            TokenManager.Instance.TokenRemovedFromScreen += OnTokenRemovedFromScreen;
        }

        void OnDisable()
        {
            TokenManager.Instance.TokenPlacedOnScreen -= OnTokenPlacedOnScreen;
            TokenManager.Instance.ScreenTokenUpdated -= OnTokenUpdated;
            TokenManager.Instance.TokenRemovedFromScreen -= OnTokenRemovedFromScreen;
        }

        // Use this for initialization
        void Start()
        {

            if (Functions != null)
            {
                //ApplicationFunctions = Functions as IApplicationToken;

            }

        }
    }
}
