/*
 * @author Francesco Strada
 */

using System.Collections.Generic;
using Touchables.Utils;
using Touchables.Utils.Attributes;

namespace Touchables.TokenEngine.TokenTypes
{
    abstract internal class TokenType
    {
        internal readonly float DistanceOriginCenterPX;
        internal readonly float DistanceOriginAxisMarkersPX;
        internal readonly float DataMarkerOriginPositionPX;
        internal readonly float DataGridMarkersStepPX;
        internal readonly float TokenDiagonalPX;

        internal readonly Dictionary<TokenDataGridCoord, int> TokenClassLUT;

        internal readonly float DataMarkerOriginPositionCM;

        protected TokenType()
        {
            DistanceOriginCenterPX = SetOriginToCenterDistance();
            DistanceOriginAxisMarkersPX = SetOriginToAxisDistance();
            DataMarkerOriginPositionPX = SetDataMarkerOriginPosition();
            DataMarkerOriginPositionCM = SetDataMarkerOriginPositionCM();
            TokenDiagonalPX = SetTokenDiagonal();

            TokenClassLUT = InitiliazeTokenClassLUT();

            DataGridMarkersStepPX = ScreenUtils.CmToPixels(TokenAttributes.TOKEN_DATA_MARKERS_STEP);
        }

        internal int? GetTokenClass(int xIndex, int yIndex)
        {
            int result;
            if (TokenClassLUT.TryGetValue(new TokenDataGridCoord(xIndex, yIndex), out result))
                return result;
            else
                return null;
        }

        internal abstract float SetOriginToCenterDistance();
        internal abstract float SetOriginToAxisDistance();
        internal abstract float SetDataMarkerOriginPosition();
        internal abstract float SetDataMarkerOriginPositionCM();
        internal abstract float SetTokenDiagonal();
        internal abstract Dictionary<TokenDataGridCoord, int> InitiliazeTokenClassLUT();

    }

    internal struct TokenDataGridCoord
    {
        int x;
        int y;

        internal TokenDataGridCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
