using System.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimationTool
{
    public class FreeRectangleChoiceHeuristic
    {
        // -BSSF: 将矩形放在自由矩形的短边上，使其最合适。
        public const int BestShortSideFit = 0;
        // -BLSF: 将矩形放在自由矩形的长边上，使其最适合。
        public const int BestLongSideFit = 1;
        // -BAF: 将矩形放置到最小的自由矩形中。
        public const int BestAreaFit = 2;
        // -BL: 左下角规则
        public const int BottomLeftRule = 3;
        // -CP: 接触点
        public const int ContactPointRule = 4;
    }

    // 矩形装箱
    class MaxRectsBinPack
    {
        public int binWidth = 0;
        public int binHeight = 0;
        //是否允许旋转角度
        public bool allowRotations = false;
        //已使用的矩形范围
        public List<Int32Rect> usedRectangles = new List<Int32Rect>();
        //空闲的矩形范围
        public List<Int32Rect> freeRectangles = new List<Int32Rect>();

        int score1 = 0;
        int score2 = 0;
        int bestShortSideFit;
        int bestLongSideFit;

        public MaxRectsBinPack( int Width, int Height, bool rotations = false)
        {
            Init(Width, Height, rotations);
        }

        public void Init(int Width, int Height, bool rotations = false)
        {
            //必须是2的幂
            if (Count(Width) % 1 != 0 ||
                Count(Height) % 1 != 0)
                return;
            binWidth = Width;
            binHeight = Height;
            allowRotations = rotations;

            var n = new Int32Rect(0, 0, Width, Height);

            usedRectangles.Clear();
            freeRectangles.Clear();
            freeRectangles.Add(n);
        }

        //检查是不是2的幂
        float Count(float n)
        {
            if (n >= 2)
                return Count(n / 2f);
            return n;
        }

        public Int32Rect insert(int width, int height, int method)
        {
            var newNode = new Int32Rect();
            score1 = 0;
            score2 = 0;
            switch(method)
            {
                case FreeRectangleChoiceHeuristic.BestShortSideFit:
                    newNode = findPositionForNewNodeBestShortSideFit(width, height);
                    break;
                case FreeRectangleChoiceHeuristic.BottomLeftRule:
                    newNode = findPositionForNewNodeBottomLeft(width, height, score1, score2);
                    break;
                case FreeRectangleChoiceHeuristic.ContactPointRule:
                    newNode = findPositionForNewNodeContactPoint(width, height, score1);
                    break;
                case FreeRectangleChoiceHeuristic.BestLongSideFit:
                    newNode = findPositionForNewNodeBestLongSideFit(width, height, score2, score1);
                    break;
                case FreeRectangleChoiceHeuristic.BestAreaFit:
                    newNode = findPositionForNewNodeBestAreaFit(width, height, score1, score2);
                    break;
            }
            if (newNode.Height == 0)
                return newNode;

            placeRectangle(newNode);
            return newNode;
        }

        public void insert2( List<Int32Rect> rectangles, List<Int32Rect> dst, int method)
        {
            dst.Clear();

            while (rectangles.Count > 0)
            {
                int bestScore1 = int.MaxValue;
                int bestScore2 = int.MaxValue;
                int bestRectangleIndex = -1;
                Int32Rect bestNode = new Int32Rect();

                for (int i = 0; i < rectangles.Count; ++i)
                {
                    int score1 = 0;
                    int score2 = 0;
                    Int32Rect newNode = scoreRectangle(rectangles[i].Width, rectangles[i].Height, method, ref score1, ref score2);

                    if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                    {
                        bestScore1 = score1;
                        bestScore2 = score2;
                        bestNode = newNode;
                        bestRectangleIndex = i;
                    }
                }

                if (bestRectangleIndex == -1)
                    return;

                placeRectangle(bestNode);
                rectangles.RemoveAt(bestRectangleIndex);
            }
        }

        //记录矩形
        void placeRectangle(Int32Rect node)
        {
            int freeNum = freeRectangles.Count;
            for( int i = 0; i < freeNum; ++i )
            {
                if( splitFreeNode(freeRectangles[i], node ) )
                {
                    freeRectangles.RemoveAt(i);
                    --i;
                    --freeNum;
                }
            }
            pruneFreeList();
            usedRectangles.Add(node);
        }

        // 得到剩余空间范围
        float occupancy()
        {
            float usedSurfaceArea = 0;
            for (int i = 0; i < usedRectangles.Count; ++i)
                usedSurfaceArea += (float)usedRectangles[i].Width * (float)usedRectangles[i].Height;
            return usedSurfaceArea / (binWidth * binHeight);
        }

        Int32Rect scoreRectangle(int width, int height, int method, ref int score1, ref int score2)
        {
            Int32Rect newNode = new Int32Rect();
            score1 = int.MaxValue;
            score2 = int.MaxValue;
            switch (method)
            {
                case FreeRectangleChoiceHeuristic.BestShortSideFit:
                    newNode = findPositionForNewNodeBestShortSideFit(width, height);
                    break;
                case FreeRectangleChoiceHeuristic.BottomLeftRule:
                    newNode = findPositionForNewNodeBottomLeft(width, height, score1, score2);
                    break;
                case FreeRectangleChoiceHeuristic.ContactPointRule:
                    newNode = findPositionForNewNodeContactPoint(width, height, score1);
                    // todo: reverse
                    score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
                    break;
                case FreeRectangleChoiceHeuristic.BestLongSideFit:
                    newNode = findPositionForNewNodeBestLongSideFit(width, height, score2, score1);
                    break;
                case FreeRectangleChoiceHeuristic.BestAreaFit:
                    newNode = findPositionForNewNodeBestAreaFit(width, height, score1, score2);
                    break;
            }
            // Cannot fit the current Rectangle.
            if (newNode.Height == 0)
            {
                score1 = int.MaxValue;
                score2 = int.MaxValue;
            }

            return newNode;
        }

        // 将矩形放在自由矩形的短边上，使其最合适。
        Int32Rect findPositionForNewNodeBestShortSideFit(int width, int height)
        {
            Int32Rect bestNode = new Int32Rect();

            bestShortSideFit = int.MaxValue;
            bestLongSideFit = score2;
            Int32Rect rect;
            int leftoverHoriz;
            int leftoverVert;
            int shortSideFit;
            int longSideFit;
            for (int i = 0; i < freeRectangles.Count; i++) {
                rect = freeRectangles[i];
                // Try to place the Rectangle in upright (non-flipped) orientation.
                if (rect.Width >= width && rect.Height >= height)
                {
                    leftoverHoriz = (int)Math.Abs(rect.Width - width);
                    leftoverVert = (int)Math.Abs(rect.Height - height);
                    shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode.X= rect.X;
                        bestNode.Y= rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
                int flippedLeftoverHoriz;
                int flippedLeftoverVert;
                int flippedShortSideFit;
                int flippedLongSideFit;
                if (allowRotations && rect.Width >= height && rect.Height >= width)
                {
                    flippedLeftoverHoriz = (int)Math.Abs(rect.Width - height);
                    flippedLeftoverVert = (int)Math.Abs(rect.Height - width);
                    flippedShortSideFit = Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                    flippedLongSideFit = Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

                    if (flippedShortSideFit < bestShortSideFit || (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = flippedShortSideFit;
                        bestLongSideFit = flippedLongSideFit;
                    }
                }
            }
            return bestNode;
        }

        // 将矩形放在自由矩形的长边上，使其最适合。
        Int32Rect findPositionForNewNodeBestLongSideFit(int width, int height, int bestShortSideFit, int bestLongSideFit)
        {
            Int32Rect bestNode = new Int32Rect();
            bestLongSideFit = int.MaxValue;
            Int32Rect rect;
            int leftoverHoriz;
            int leftoverVert;
            int shortSideFit;
            int longSideFit;
            for (int i = 0; i < freeRectangles.Count; i++) 
            {
                rect = freeRectangles[i];
                // Try to place the Rectangle in upright (non-flipped) orientation.
                if (rect.Width >= width && rect.Height >= height)
                {
                    leftoverHoriz = (int)Math.Abs(rect.Width - width);
                    leftoverVert = (int)Math.Abs(rect.Height - height);
                    shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X= rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (allowRotations && rect.Width >= height && rect.Height >= width)
                {
                    leftoverHoriz = (int)Math.Abs(rect.Width - height);
                    leftoverVert = (int)Math.Abs(rect.Height - width);
                    shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
            }
            return bestNode;
        }

        // 将矩形放置到最小的自由矩形中。
        private Int32Rect findPositionForNewNodeBestAreaFit(int width, int height, int bestAreaFit, int bestShortSideFit)
        {
            Int32Rect bestNode = new Int32Rect();
            bestAreaFit = int.MaxValue;
            Int32Rect rect;
            int leftoverHoriz;
            int leftoverVert;
            int shortSideFit;
            int areaFit;

            for (int i = 0; i < freeRectangles.Count; i++) 
            {
                rect = freeRectangles[i];
                areaFit = (int)(rect.Width * rect.Height - width * height);

                // Try to place the Rectangle in upright (non-flipped) orientation.
                if (rect.Width >= width && rect.Height >= height)
                {
                    leftoverHoriz = (int)Math.Abs(rect.Width - width);
                    leftoverVert = (int)Math.Abs(rect.Height - height);
                    shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X= rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }

                if (allowRotations && rect.Width >= height && rect.Height >= width)
                {
                    leftoverHoriz = (int)Math.Abs(rect.Width - height);
                    leftoverVert = (int)Math.Abs(rect.Height - width);
                    shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }
            }
            return bestNode;
        }

        // 左下角规则
        Int32Rect findPositionForNewNodeBottomLeft( int width, int height, int bestY, int bestX )
        {
            Int32Rect bestNode = new Int32Rect();
            bestY = int.MaxValue;
            Int32Rect rect;
            int topSideY;
            for( int i = 0; i < freeRectangles.Count; ++i )
            {
                rect = freeRectangles[i];
                if( rect.Width >= width &&
                    rect.Height >= height)
                {
                    topSideY = (int)(rect.Y + height);
                    if( topSideY < bestY ||
                        ( topSideY == bestY && rect.X < bestX ))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestY = topSideY;
                        bestX = (int)rect.X;
                    }
                }
                if (allowRotations && rect.Width >= height && rect.Height >= width)
                {
                    topSideY = (int)(rect.Y + width);
                    if (topSideY < bestY || (topSideY == bestY && rect.X < bestX))
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestY = topSideY;
                        bestX = (int)rect.X;
                    }
                }
            }
            return bestNode;
        }

        // 接触点规则
        Int32Rect findPositionForNewNodeContactPoint( int width, int height, int bestContactScore)
        {
            Int32Rect bestNode = new Int32Rect();
            bestContactScore = -1;
            Int32Rect rect;
            int score;
            for( int i = 0; i <freeRectangles.Count; ++i )
            {
                rect = freeRectangles[i];
                if( rect.Width >= width && rect.Height >= height )
                {
                    score = contactPointScoreNode((int)rect.X, (int)rect.Y, width, width);
                    if( score > bestContactScore )
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = width;
                        bestNode.Height = height;
                        bestContactScore = score;
                    }
                }
                if( allowRotations && rect.Width >= height && rect.Height >= width )
                {
                    score = contactPointScoreNode((int)rect.X, (int)rect.Y, height, width);
                    if (score > bestContactScore)
                    {
                        bestNode.X = rect.X;
                        bestNode.Y = rect.Y;
                        bestNode.Width = height;
                        bestNode.Height = width;
                        bestContactScore = score;
                    }
                }
            }
            return bestNode;
        }

        /// 判断相交分值
        int commonIntervalLength(int i1start, int i1end, int i2start, int i2end)
        {
            if (i1end < i2start || i2end < i1start)
                return 0;
            return Math.Min(i1end, i2end) - Math.Max(i1start, i2start);
        }

        // 计算矩形插入的 插入分值
        int contactPointScoreNode(int x, int y, int width, int height)
        {
            int score = 0;
            if (x == 0 || x + width == binWidth)
                score += height;
            if (y == 0 || y + height == binHeight)
                score += width;
            Int32Rect rect;
            for( int i = 0; i < usedRectangles.Count; ++i )
            {
                rect = usedRectangles[i];
                if (rect.X == x + width || rect.X + rect.Width == x)
                    score += commonIntervalLength((int)rect.Y, (int)(rect.Y + rect.Height), y, y + height);
                if ( rect.Y  == y + height || rect.Y + rect.Height == y )
                    score += commonIntervalLength((int)rect.X, (int)(rect.X + rect.Width), x, x + width);
            }
            return score;
        }

        // 分裂空闲矩形节点
        bool splitFreeNode(Int32Rect freeNode, Int32Rect usedNode)
        {
            if (usedNode.X >= freeNode.X + freeNode.Width || usedNode.X + usedNode.Width <= freeNode.X ||
                usedNode.Y >= freeNode.Y + freeNode.Height || usedNode.Y + usedNode.Height <= freeNode.Y)
                return false;
            Int32Rect newNode;
            if (usedNode.X < freeNode.X + freeNode.Width && usedNode.X + usedNode.Width > freeNode.X)
            {
                // 顶部创建新矩形节点
                if (usedNode.Y > freeNode.Y && usedNode.Y < freeNode.Y + freeNode.Height)
                {
                    newNode = new Int32Rect(freeNode.X, freeNode.Y, freeNode.Width, freeNode.Height);
                    newNode.Height = usedNode.Y - newNode.Y;
                    freeRectangles.Add(newNode);
                }

                // 底部创建新矩形节点
                if (usedNode.Y + usedNode.Height < freeNode.Y + freeNode.Height)
                {
                    newNode = new Int32Rect(freeNode.X, freeNode.Y, freeNode.Width, freeNode.Height);
                    newNode.Y = usedNode.Y + usedNode.Height;
                    newNode.Height = freeNode.Y + freeNode.Height - (usedNode.Y + usedNode.Height);
                    freeRectangles.Add(newNode);
                }
            }

            if (usedNode.Y < freeNode.Y + freeNode.Height && usedNode.Y + usedNode.Height > freeNode.Y)
            {
                // 左边创建新节点
                if (usedNode.X > freeNode.X && usedNode.X < freeNode.X + freeNode.Width)
                {
                    newNode = new Int32Rect(freeNode.X, freeNode.Y, freeNode.Width, freeNode.Height);
                    newNode.Width = usedNode.X - newNode.X;
                    freeRectangles.Add(newNode);
                }

                // 右边创建新节点
                if (usedNode.X + usedNode.Width < freeNode.X + freeNode.Width)
                {
                    newNode = new Int32Rect(freeNode.X, freeNode.Y, freeNode.Width, freeNode.Height);
                    newNode.X = usedNode.X + usedNode.Width;
                    newNode.Width = freeNode.X + freeNode.Width - (usedNode.X + usedNode.Width);
                    freeRectangles.Add(newNode);
                }
            }
            return true;
        }

        // 移除空闲列表
        void pruneFreeList()
        {
            for( int i = 0; i < freeRectangles.Count; ++i )
            {
                for( int j = i + 1;  j < freeRectangles.Count; ++j )
                {
                    if( isContainedIn(freeRectangles[i], freeRectangles[j] ) )
                    {
                        freeRectangles.RemoveAt(i);
                        --i;
                        break;
                    }
                    if (isContainedIn(freeRectangles[j], freeRectangles[i]))
                    {
                        freeRectangles.RemoveAt(j);
                        --j;
                    }
                }
            }
        }

        // 是否碰撞
        bool isContainedIn( Int32Rect a, Int32Rect b )
        {
            return a.X > b.X && a.Y >= b.Y &&
                       a.X + a.Width <= b.X + b.Width &&
                       a.Y + a.Height <= b.Y + b.Height;
        }
    }
}
