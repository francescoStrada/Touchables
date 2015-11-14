/*
 * @author Francesco Strada
 */

using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using Touchables.TokenEngine;
using Touchables.MultiTouchManager;

namespace Touchables.Utils
{
    internal static class TokenUtils
    {
        /// <summary>
        /// Converts a series of TouchInputs present in a cluster to TokenMarkers with specific <see cref="MarkerType"/>
        /// </summary>
        /// <param name="orderedIndexes">Array of indexes ordered according to <see cref="MarkerType"/> </param>
        /// <param name="clusterPoints">Dictionary of TouchInputs present in a cluster</param>
        /// <returns>A dictionary of TokenMarkers with their ID as key</returns>
        public static Dictionary<int, TokenMarker> ConvertTouchInputToMarkers(int[] orderedIndexes, Dictionary<int, TouchInput> clusterPoints)
        {
            Dictionary<int, TokenMarker> result = new Dictionary<int, TokenMarker>();

            for (int i = 0; i < orderedIndexes.Length; i++)
            {
                TouchInput ti = clusterPoints[orderedIndexes[i]];

                // ordered index is organized:
                // [0] => Origin
                // [1] => XAxis
                // [2] => YAxis
                // [3] => Data

                switch (i)
                {
                    case 0:
                        {
                            TokenMarker tm = new TokenMarker(ti.Id, ti.Position, ti.State, MarkerType.Origin);
                            result.Add(tm.Id, tm);
                            break;
                        }
                    case 1:
                        {
                            TokenMarker tm = new TokenMarker(ti.Id, ti.Position, ti.State, MarkerType.XAxis);
                            result.Add(tm.Id, tm);
                            break;
                        }
                    case 2:
                        {
                            TokenMarker tm = new TokenMarker(ti.Id, ti.Position, ti.State, MarkerType.YAxis);
                            result.Add(tm.Id, tm);
                            break;
                        }
                    case 3:
                        {
                            TokenMarker tm = new TokenMarker(ti.Id, ti.Position, ti.State, MarkerType.Data);
                            result.Add(tm.Id, tm);
                            break;
                        }
                }
            }

            return result;
        }

        /// <summary>
        /// Computes the closest orthogonal reference system to the one originally detected with a mean square solution
        /// </summary>
        /// <param name="originalOrigin">Originally detected Origin marker</param>
        /// <param name="originalXAxis">Originally detected X Axis marker</param>
        /// <param name="originalYAxis">Originally detected Y Axis marker</param>
        /// <param name="originToAxisDistance"> Distance between Origin marker and Axis markers</param>
        /// <returns>Array of TokenMarkers reppresdenting the closest orthgoanl reference system with respect to the specified one</returns>
        public static TokenMarker[] MeanSquareOrthogonalReferenceSystem(TokenMarker originalOrigin, TokenMarker originalXAxis, TokenMarker originalYAxis, float originToAxisDistance)
        {
            TokenMarker[] result = new TokenMarker[3];
            var M = Matrix<float>.Build;


            float[,] arrayA = { { 0.0f, 0.0f, 1.0f, 0.0f },
                              { 0.0f, 0.0f, 0.0f, 1.0f },
                              { originToAxisDistance, 0.0f, 1.0f, 0.0f },
                              { 0.0f, originToAxisDistance, 0.0f, 1.0f },
                              { 0.0f, -originToAxisDistance, 1.0f, 0.0f},
                              { originToAxisDistance, 0.0f, 0.0f, 1.0f } };

            float[,] arrayB = { { originalOrigin.Position.x },
                                { originalOrigin.Position.y },
                                { originalXAxis.Position.x },
                                { originalXAxis.Position.y },
                                { originalYAxis.Position.x },
                                { originalYAxis.Position.y} };

            var A = M.DenseOfArray(arrayA);
            var b = M.DenseOfArray(arrayB);

            var x = A.TransposeThisAndMultiply(A).Inverse() * A.TransposeThisAndMultiply(b);

            float[,] transformationMatrix = x.ToArray();

            Vector2 newOrigin = new Vector2(transformationMatrix[2, 0],
                                             transformationMatrix[3, 0]);
            Vector2 newXAxis = new Vector2(originToAxisDistance * transformationMatrix[0, 0] + transformationMatrix[2, 0],
                                            originToAxisDistance * transformationMatrix[1, 0] + transformationMatrix[3, 0]);
            Vector2 newYAxis = new Vector2(-originToAxisDistance * transformationMatrix[1, 0] + transformationMatrix[2, 0],
                                             originToAxisDistance * transformationMatrix[0, 0] + transformationMatrix[3, 0]);

            result[0] = new TokenMarker(originalOrigin.Id, newOrigin, originalOrigin.State, MarkerType.Origin);
            result[1] = new TokenMarker(originalXAxis.Id, newXAxis, originalXAxis.State, MarkerType.XAxis);
            result[2] = new TokenMarker(originalYAxis.Id, newYAxis, originalYAxis.State, MarkerType.YAxis);

            return result;
        }

        /// <summary>
        /// Computes a RotoTranslation transformation
        /// </summary>
        /// <param name="originalPosition">Point to transform position in old reference system</param>
        /// <param name="newOriginPosition">New reference system origin position with respect to old one</param>
        /// <param name="angleRad">New reference system rotation with respect to old one </param>
        /// <returns>Position of specified point with respect to new reference system</returns>
        public static Vector2 ComputeRotoTranslation(Vector2 originalPosition, Vector2 newOriginPosition, float angleRad)
        {
            var M = Matrix<float>.Build;
            var v = Vector<float>.Build;
            float angleCos = Mathf.Cos(angleRad);
            float angleSin = Mathf.Sin(angleRad);

            float[,] rotation = { { angleCos , angleSin },
                                  { -angleSin, angleCos }};

            float[] translation = { originalPosition.x - newOriginPosition.x, originalPosition.y - newOriginPosition.y };

            var R = M.DenseOfArray(rotation);
            var T = v.DenseOfArray(translation);

            float[] result = (R * T).ToArray();

            return new Vector2(result[0], result[1]);

        }
    }
}
