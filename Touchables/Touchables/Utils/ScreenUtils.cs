/*
 * @author Francesco Strada 
 */

using UnityEngine;

namespace Touchables.Utils
{
    /// <summary>
    /// Utils functions for touch screen related operations
    /// </summary>
    internal static class ScreenUtils
    {
        /// <summary>
        /// Converts pixels value to centimeters according to screen dpi through Unity's Screen. <seealso cref="Screen"/>
        /// </summary>
        /// <param name="pxValue">Value in pixels</param>
        /// <returns>Equivalent value in centimeters</returns>
        public static float PixelsToCm(float pxValue)
        {
            return (pxValue / Screen.dpi) * 2.54f;
        }

        /// <summary>
        /// Converts centimeters value to pixels according to screen dpi through Unity's Screen.dpi. <seealso cref="Screen"/>
        /// </summary>
        /// <param name="cmValue">Value in centimeters</param>
        /// <returns>Equivalente value in pixels</returns>
        public static float CmToPixels(float cmValue)
        {
            return ((cmValue * Screen.dpi) / 2.54f);
        }
    }
}
