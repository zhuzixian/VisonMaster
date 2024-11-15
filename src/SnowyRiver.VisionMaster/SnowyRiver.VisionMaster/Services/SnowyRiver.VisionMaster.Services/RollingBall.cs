using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace SnowyRiver.VisionMaster.Services;
public enum Direction
{
    X_DIRECTION,
    Y_DIRECTION,
    DIAGONAL_1A,
    DIAGONAL_1B,
    DIAGONAL_2A,
    DIAGONAL_2B
}
public class RollingBall
{
    public int c_shrink_factor;
    public int c_width;
    public List<float> c_data;
    int radius;
    public RollingBall()
    {

    }
    public RollingBall(int radius)
    {
        this.radius = radius;
        int trim_para;
        if (radius <= 10)
        {
            c_shrink_factor = 1;
            trim_para = 24;
        }
        else if (radius <= 30)
        {
            c_shrink_factor = 2;
            trim_para = 24;
        }
        else if (radius <= 100)
        {
            c_shrink_factor = 4;
            trim_para = 32;
        }
        else
        {
            c_shrink_factor = 8;
            trim_para = 40;
        }
        c_data = new List<float>();
        BuildRollingBall(radius, trim_para);
    }

    private void BuildRollingBall(float ball_radius, int trim_para)
    {
        double rsquare = 0;  //半径平方
        int xtrim = 0;
        int xval = 0, yval = 0;
        double small_ball_radius = ball_radius / c_shrink_factor;  //下采样后求的半径，只有图片缩放后才会启用
        int half_width;

        if (small_ball_radius < 1)
        {
            small_ball_radius = 1;
        }
        rsquare = small_ball_radius * small_ball_radius;
        xtrim = (int)(trim_para * small_ball_radius) / 100;
        half_width = (int)Math.Round(small_ball_radius - xtrim);
        c_width = 2 * half_width + 1;

        for (int y = 0; y < c_width; y++)
        {
            for (int x = 0; x < c_width; x++)
            {
                xval = x - half_width;
                yval = y - half_width;
                float temp = (float)(rsquare - (float)xval * xval - (float)yval * yval);
                float val = temp > 0 ? (float)Math.Sqrt(temp) : 0;
                c_data.Add(val);
            }
        }
    }
    private static void Smooth(Mat src, Mat dst, int kernel_size = 3)
    {
        Cv2.MedianBlur(src, dst, kernel_size);
    }
    static unsafe float[] LineSlideParabola(float* pixels, int start, int inc, int length, float coeff2, float[] cache, int[] nextPoint, float[] correctedEdges)
    {
        float minValue = float.MaxValue;
        int lastpoint = 0;
        int firstCorner = length - 1;
        int lastCorner = 0;
        float vPrevious1 = 0;
        float vPrevious2 = 0;
        float curvatureTest = 1.999f * coeff2;

        for (int i = 0, p = start; i < length; i++, p += inc)
        {
            float v = pixels[p];
            cache[i] = v;
            if (v < minValue) minValue = v;   //曲率经验公式
            if (i >= 2 && vPrevious1 + vPrevious1 - vPrevious2 - v < curvatureTest)
            {
                nextPoint[lastpoint] = i - 1;
                lastpoint = i - 1;
            }
            vPrevious2 = vPrevious1;
            vPrevious1 = v;
        }
        nextPoint[lastpoint] = length - 1;
        nextPoint[length - 1] = int.MaxValue;
        int i1 = 0;
        while (i1 < length - 1)
        {
            float v1 = cache[i1];
            float minSlope = float.MaxValue;
            int i2 = 0;
            int searchTo = length;
            int recalculateLimitNow = 0;
            for (int j = nextPoint[i1]; j < searchTo; j = nextPoint[j], recalculateLimitNow++)
            {
                float v2 = cache[j];
                float slope = (v2 - v1) / (j - i1) + coeff2 * (j - i1);
                if (slope < minSlope)
                {
                    minSlope = slope;
                    i2 = j;
                    recalculateLimitNow = -3;
                }
                if (recalculateLimitNow == 0)
                {
                    double b = 0.5 * minSlope / coeff2;
                    int maxSearch = i1 + (int)(b + Math.Sqrt(b * b + (v1 - minValue) / coeff2) + 1);
                    if (maxSearch < searchTo && maxSearch > 0) searchTo = maxSearch;
                }
            }
            if (i1 == 0) firstCorner = i2;
            if (i2 == length - 1) lastCorner = i1;
            for (int j = i1 + 1, p = start + j * inc; j < i2; j++, p += inc)
                pixels[p] = v1 + (j - i1) * (minSlope - (j - i1) * coeff2);
            i1 = i2;
        }

        if (correctedEdges != null)
        {
            if (4 * firstCorner >= length) firstCorner = 0;
            if (4 * (length - 1 - lastCorner) >= length) lastCorner = length - 1;

            float v1 = cache[firstCorner];
            float v2 = cache[lastCorner];
            float slope = (v2 - v1) / (lastCorner - firstCorner);
            float value0 = v1 - slope * firstCorner;

            float coeff6 = 0;
            float mid = 0.5f * (lastCorner + firstCorner);
            for (int i = (length + 2) / 3; i <= (2 * length) / 3; i++)
            {
                float dx1 = ((float)i - mid) * 2.0f / ((float)lastCorner - firstCorner);
                float poly6 = dx1 * dx1 * dx1 * dx1 * dx1 * dx1 - 1.0f;
                if (cache[i] < value0 + slope * i + coeff6 * poly6)
                {
                    coeff6 = -(value0 + slope * i - cache[i]) / poly6;
                }
            }
            float dx = ((float)firstCorner - mid) * 2.0f / ((float)lastCorner - firstCorner);
            correctedEdges[0] = value0 + coeff6 * ((float)dx * dx * dx * dx * dx * dx - 1.0f) + (float)coeff2 * firstCorner * firstCorner;
            dx = ((float)lastCorner - mid) * 2.0f / ((float)lastCorner - firstCorner);
            correctedEdges[1] = value0 + (length - 1) * slope + coeff6 * ((float)dx * dx * dx * dx * dx * dx - 1.0f) + (float)coeff2 * (length - 1 - lastCorner) * (length - 1 - lastCorner);

        }

        return correctedEdges;
    }
    private float[] GetFloatArray(Mat mat)
    {
        var floatMat = new Mat();
        mat.ConvertTo(floatMat, MatType.CV_32FC1);
        floatMat.GetArray(out float[] floatArray);
        return floatArray;
    }

    private unsafe void Filter1D(Mat src, Direction direction, float coeff2, float[] cache, int[] nextPoint)
    {
        //float[] pixels = GetFloatArray(src);
        float* pixels = (float*)src.DataPointer;
        int width = src.Cols;
        int height = src.Rows;
        int startLine = 0;
        int nLines = 0;
        int lineInc = 0;
        int pointInc = 0;
        int length = 0;
        switch (direction)
        {
            case Direction.X_DIRECTION:
                nLines = height;
                lineInc = width;
                pointInc = 1;
                length = width;
                break;
            case Direction.Y_DIRECTION:
                nLines = width;
                lineInc = 1;
                pointInc = width;
                length = height;
                break;
            case Direction.DIAGONAL_1A:
                nLines = width - 2; // the algorithm makes no sense for lines shorter than 3 pixels
                lineInc = 1;
                pointInc = width + 1;
                break;
            case Direction.DIAGONAL_1B:
                startLine = 1;
                nLines = height - 2;
                lineInc = width;
                pointInc = width + 1;
                break;
            case Direction.DIAGONAL_2A:
                startLine = 2;
                nLines = width;
                lineInc = 1;
                pointInc = width - 1;
                break;
            case Direction.DIAGONAL_2B:
                startLine = 0;
                nLines = height - 2;
                lineInc = width;
                pointInc = width - 1;
                break;
        }
        for (int i = startLine; i < nLines; i++)
        {
            int startPixel = i * lineInc;
            if (direction == Direction.DIAGONAL_2B)
                startPixel += width - 1;

            switch (direction)
            {
                case Direction.DIAGONAL_1A:
                    length = Math.Min(height, width - i);
                    break;
                case Direction.DIAGONAL_1B:
                    length = Math.Min(width, height - i);
                    break;
                case Direction.DIAGONAL_2A:
                    length = Math.Min(height, i + 1);
                    break;
                case Direction.DIAGONAL_2B:
                    length = Math.Min(width, height - i);
                    break;
            }

            LineSlideParabola(pixels, startPixel, pointInc, length, coeff2, cache, nextPoint, null);
        }
        //dst = new Mat(src.Size(),src.Type());
        ////src.SetArray(pixels);
        //dst = src.Clone();

    }
    public unsafe void CorrectCorners(Mat src, float coeff2, float[] cache, int[] nextPoint)
    {
        int width = src.Cols;
        int height = src.Rows;
        float* pixels = (float*)src.DataPointer; // 获取浮点数数组
        float[] corners = new float[4];      // (0,0) (width-1,0) (0,height-1) (width-1,height-1)
        float[] correctedEdges = new float[2];

        // 第一行
        correctedEdges = LineSlideParabola(pixels, 0, 1, width, coeff2, cache, nextPoint, correctedEdges);
        corners[0] = correctedEdges[0];
        corners[1] = correctedEdges[1];

        // 最后一行
        correctedEdges = LineSlideParabola(pixels, (height - 1) * width, 1, width, coeff2, cache, nextPoint, correctedEdges);
        corners[2] = correctedEdges[0];
        corners[3] = correctedEdges[1];

        // 第一列
        correctedEdges = LineSlideParabola(pixels, 0, width, height, coeff2, cache, nextPoint, correctedEdges);
        corners[0] += correctedEdges[0];
        corners[2] += correctedEdges[1];

        // 最后一列
        correctedEdges = LineSlideParabola(pixels, width - 1, width, height, coeff2, cache, nextPoint, correctedEdges);
        corners[1] += correctedEdges[0];
        corners[3] += correctedEdges[1];

        int diagLength = Math.Min(width, height); // 对角线长度（长宽不等时取最小值）
        float coeff2diag = 2 * coeff2;

        // 左上到右下（长宽相等则正好到右下）
        correctedEdges = LineSlideParabola(pixels, 0, 1 + width, diagLength, coeff2diag, cache, nextPoint, correctedEdges);
        corners[0] += correctedEdges[0];

        // 右上到左下
        correctedEdges = LineSlideParabola(pixels, width - 1, -1 + width, diagLength, coeff2diag, cache, nextPoint, correctedEdges);
        corners[1] += correctedEdges[0];

        // 左下到右上
        correctedEdges = LineSlideParabola(pixels, (height - 1) * width, 1 - width, diagLength, coeff2diag, cache, nextPoint, correctedEdges);
        corners[2] += correctedEdges[0];

        // 右下到左上
        correctedEdges = LineSlideParabola(pixels, width * height - 1, -1 - width, diagLength, coeff2diag, cache, nextPoint, correctedEdges);
        corners[3] += correctedEdges[0];

        if (pixels[0] > corners[0] / 3) pixels[0] = corners[0] / 3;
        if (pixels[width - 1] > corners[1] / 3) pixels[width - 1] = corners[1] / 3;
        if (pixels[(height - 1) * width] > corners[2] / 3) pixels[(height - 1) * width] = corners[2] / 3;
        if (pixels[width * height - 1] > corners[3] / 3) pixels[width * height - 1] = corners[3] / 3;
        //src.SetArray(pixels);

    }
    public unsafe void SlidingParaboloidFloatBackground(Mat src, float radius, out Mat backgroundImg, bool correctCorner)
    {
        Mat srcCopy = src.Clone();
        float* pixels = (float*)src.DataPointer;
        int width = srcCopy.Cols;
        int height = srcCopy.Rows;
        List<float> cache = new List<float>(Math.Max(width, height));
        List<int> nextPoint = new List<int>(Math.Max(width, height));   // 是否数组更好
        float coeff2 = 0.5f / radius;                 // 二项式阶数，越小越尖锐
        float coeff2diag = 1.0f / radius;             // 对角线上二项式阶数

        //初始化缓存和下一个点列表
        for (int i = 0; i < Math.Max(width, height); i++)
        {
            cache.Add(0.0f);
            nextPoint.Add(0);
        }

        if (correctCorner)
        {
            CorrectCorners(srcCopy, coeff2, cache.ToArray(), nextPoint.ToArray());
        }

        // 沿不同方向滑动抛物线
        Filter1D(srcCopy, Direction.X_DIRECTION, coeff2, cache.ToArray(), nextPoint.ToArray());
        Filter1D(srcCopy, Direction.Y_DIRECTION, coeff2, cache.ToArray(), nextPoint.ToArray());
        Filter1D(srcCopy, Direction.X_DIRECTION, coeff2, cache.ToArray(), nextPoint.ToArray()); // 重做以提高精度
        Filter1D(srcCopy, Direction.DIAGONAL_1A, coeff2diag, cache.ToArray(), nextPoint.ToArray());
        Filter1D(srcCopy, Direction.DIAGONAL_1B, coeff2diag, cache.ToArray(), nextPoint.ToArray());
        Filter1D(srcCopy, Direction.DIAGONAL_2A, coeff2diag, cache.ToArray(), nextPoint.ToArray());
        Filter1D(srcCopy, Direction.DIAGONAL_2B, coeff2diag, cache.ToArray(), nextPoint.ToArray());
        Filter1D(srcCopy, Direction.DIAGONAL_1A, coeff2diag, cache.ToArray(), nextPoint.ToArray()); // 重做以提高精度
        Filter1D(srcCopy, Direction.DIAGONAL_1B, coeff2diag, cache.ToArray(), nextPoint.ToArray());

        backgroundImg = srcCopy;
    }

    public unsafe void ShrinkImage(Mat src, out Mat dst, int shrinkFactor)
    {
        int height = src.Height;
        int width = src.Width;
        // 向上取整
        int sHeight = (height + shrinkFactor - 1) / shrinkFactor;
        int sWidth = (width + shrinkFactor - 1) / shrinkFactor;

        // 创建一个新的空白图像
        dst = new Mat(sHeight, sWidth, MatType.CV_32FC1, new Scalar(0));

        float* data = (float*)dst.DataPointer;
        float* pixels = (float*)src.DataPointer;
        float min, thispixel;
        for (int ySmall = 0; ySmall < sHeight; ySmall++)
        {
            for (int xSmall = 0; xSmall < sWidth; xSmall++)
            {
                min = float.MaxValue;
                for (int j = 0, y = shrinkFactor * ySmall; j < shrinkFactor && y < height; j++, y++)
                {
                    for (int k = 0, x = shrinkFactor * xSmall; k < shrinkFactor && x < width; k++, x++)
                    {
                        thispixel = pixels[x + y * width];
                        if (thispixel < min)
                            min = thispixel;
                    }
                }
                data[xSmall + ySmall * sWidth] = min;
            }
        }
    }
    private void InterpolationArrays(int[] pSmallIndex, float[] weight, int length, int smallLength, int shrinkFactor)
    {
        for (int i = 0; i < length; i++)
        {
            int smallIndex = (i - shrinkFactor / 2) / shrinkFactor;
            if (smallIndex >= smallLength - 1) smallIndex = smallLength - 2;
            pSmallIndex[i] = smallIndex;
            float distance = (i + 0.5f) / shrinkFactor - (smallIndex + 0.5f);
            weight[i] = 1.0f - distance;
        }
    }
    private unsafe void EnlargeImg(Mat src, Mat dst, int enlargeFactor)
    {
        int height = src.Rows;
        int width = src.Cols;
        int lHeight = dst.Rows;
        int lWidth = dst.Cols;
        Mat temp = new Mat(dst.Size(), MatType.CV_32FC1, new Scalar(0));
        float* src_data = (float*)src.DataPointer;
        float* dstData = (float*)temp.DataPointer;

        float[] line0 = new float[lWidth];
        float[] line1 = new float[lWidth];

        int[] xIndices = new int[lWidth];
        float[] xWeight = new float[lWidth];
        InterpolationArrays(xIndices, xWeight, lWidth, width, enlargeFactor);
        int[] yIndices = new int[lHeight];
        float[] yWeight = new float[lHeight];
        InterpolationArrays(yIndices, yWeight, lHeight, height, enlargeFactor);

        for (int x = 0; x < lWidth; x++)
            line1[x] = src_data[xIndices[x]] * xWeight[x] + src_data[xIndices[x] + 1] * (1.0f - xWeight[x]);
        int ySmallLine0 = -1;
        for (int y = 0; y < lHeight; y++)
        {
            if (ySmallLine0 < yIndices[y])
            {
                float[] swap = line0;
                line0 = line1;
                line1 = swap;
                ySmallLine0++;
                int syPointer = (yIndices[y] + 1) * width;
                for (int x = 0; x < lWidth; x++)
                    line1[x] = src_data[syPointer + xIndices[x]] * xWeight[x] + src_data[syPointer + xIndices[x] + 1] * (1.0f - xWeight[x]);
            }
            float weight = yWeight[y];
            for (int x = 0, p = y * lWidth; x < lWidth; x++, p++)
                dstData[p] = line0[x] * weight + line1[x] * (1.0f - weight);
        }
        dst = temp.Clone();
    }



    unsafe void RollBall(Mat shrinkImg, RollingBall ball, out Mat backgroundImg)
    {
        int width = shrinkImg.Cols;
        int height = shrinkImg.Rows;
        float[] pixData = GetFloatArray(shrinkImg);
        List<float> zData = ball.c_data;
        int ballWidth = ball.c_width;
        int radius = ballWidth / 2;
        float[] cacheData = new float[width * ballWidth];
        for (int y = -radius; y < height + radius; y++)
        {
            int nextLine2WriteCache = (y + radius) % ballWidth;
            int nextLine2Read = y + radius;
            if (nextLine2Read < height && nextLine2Read >= 0)
            {
                //Buffer.MemoryCopy(pixData, )
                Array.Copy(pixData, nextLine2Read * width, cacheData, nextLine2WriteCache * width, width);
                for (int i = 0, p = nextLine2Read * width; i < width; i++, p++)
                {
                    pixData[p] = float.MinValue;
                }
            }
            int y0 = y - radius;
            y0 = y0 < 0 ? 0 : y0;
            int yBall0 = y0 - y + radius;
            int yEnd = y + radius;
            if (yEnd >= height)
                yEnd = height - 1;
            for (int x = -radius; x < width + radius; x++)
            {
                double z = double.MaxValue;
                int x0 = x - radius;
                x0 = x0 < 0 ? 0 : x0;
                int xBall0 = x0 - x + radius;
                int xEnd = x + radius;
                if (xEnd >= width) xEnd = width - 1;
                for (int yp = y0, yBall = yBall0; yp <= yEnd; yp++, yBall++)
                {
                    int cachePointer = (yp % ballWidth) * width + x0;
                    for (int xp = x0, bp = xBall0 + yBall * ballWidth; xp <= xEnd; xp++, cachePointer++, bp++)
                    {
                        float zReduced = cacheData[cachePointer] - zData[bp];
                        if (z > zReduced) z = zReduced;
                    }
                }
                for (int yp = y0, yBall = yBall0; yp <= yEnd; yp++, yBall++)
                    for (int xp = x0, p = xp + yp * width, bp = xBall0 + yBall * ballWidth; xp <= xEnd; xp++, p++, bp++)
                    {
                        float zMin = (float)(z + zData[bp]);
                        if (pixData[p] < zMin) pixData[p] = zMin;
                    }

            }
        }
        backgroundImg = new Mat(shrinkImg.Size(), MatType.CV_8UC1);
        //int[] ints = new int[width * height];
        //for (int i = 0; i < height; i++)
        //{
        //    for (int j = 0; j < width; j++)
        //    {
        //        shrinkImg.At<float>(i, j) = pixData[i * width + j];
        //    }
        //}
        shrinkImg.SetArray(pixData);
        shrinkImg.ConvertTo(backgroundImg, MatType.CV_8UC1);
        //float[] test = GetFloatArray(backgroundImg);
        //backgroundImg.ConvertTo(backgroundImg, MatType.CV_8UC1);
    }

    void RollingBallFloatBackground(Mat src, RollingBall ball, out Mat backgroundImg)
    {
        backgroundImg = new Mat();
        Mat shrImg = new Mat();
        Mat back = new Mat();
        int height = src.Height;
        int width = src.Width;
        int sHeight = (height + ball.c_shrink_factor - 1) / ball.c_shrink_factor;
        int sWidth = (width + c_shrink_factor - 1) / c_shrink_factor;
        //ShrinkImage(src, out shrImg, ball.c_shrink_factor);
        //Mat sub = new Mat();
        Cv2.Resize(src, shrImg, new Size(sWidth, sHeight), interpolation: InterpolationFlags.Lanczos4);
        RollBall(shrImg, ball, out back);
        Cv2.Resize(back, backgroundImg, new Size(width, height), interpolation: InterpolationFlags.Lanczos4);
        //Cv2.ImWrite("E:/back.png", back);
        //Cv2.ImWrite("E:/shr.png", shrImg);


    }
    /// <summary>
    /// 输入图像，输出扣除背景Mat以及背景mat。如果背景为黑色背景，Islightback设置为true，若需要图像平滑，issmooth可设置为true,若为抛物面，可设置paraboloid为true
    /// </summary>
    /// <param name="src"></param>
    /// <param name="rollBackImg"></param>
    /// <param name="backgroundImg"></param>
    /// <param name="isLightBack"></param>
    /// <param name="isSmooth"></param>
    /// <param name="isParaboloid"></param>
    public void SubtractBackgroundRollingBall(Mat src, out Mat rollBackImg, out Mat backgroundImg, bool isLightBack, bool isSmooth, bool isParaboloid)
    {

        if (isSmooth)
        {
            Smooth(src, src);
        }

        if (isLightBack)
        {
            Cv2.BitwiseNot(src, src);
        }

        Mat srcCopy = src.Clone();
        src.ConvertTo(srcCopy, MatType.CV_32FC1);

        if (isParaboloid)
        {
            SlidingParaboloidFloatBackground(srcCopy, radius, out backgroundImg, false);
        }
        else
        {
            RollingBall ballObj = new RollingBall(radius);
            RollingBallFloatBackground(srcCopy, ballObj, out backgroundImg);
        }
        rollBackImg = new Mat();
        backgroundImg.ConvertTo(backgroundImg, MatType.CV_8UC1);
        Cv2.Subtract(src, backgroundImg, rollBackImg);

    }


}





