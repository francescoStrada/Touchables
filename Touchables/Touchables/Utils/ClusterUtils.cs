/*
 * @author Francesco Strada
 */
using System;
using System.Text;

namespace Touchables.Utils
{
    /// <summary>
    /// Utility functions for Cluster related operations
    /// </summary>
    internal static class ClusterUtils
    {
        private static StringBuilder hashString = new StringBuilder();

        /// <summary>
        /// Given an Array of ints, reppresenting touch points ids, it formats them in a string like #x#y... unique Hash
        /// </summary>
        /// <param name="pointIds">Array of touch point ids</param>
        /// <returns>Hash string</returns>
        public static String GetPointsHash(int[] pointIds)
        {
            hashString.Remove(0, hashString.Length);
            for (int i = 0; i < pointIds.Length; i++)
            {
                hashString.Append("#");
                hashString.Append(pointIds[i]);
            }
            return hashString.ToString();
        }
    }
}
