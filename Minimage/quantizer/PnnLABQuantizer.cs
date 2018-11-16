﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace PnnQuant
{
    public class PnnLABQuantizer : PnnQuantizer
    {
        private Dictionary<int, CIELABConvertor.Lab> pixelMap = new Dictionary<int, CIELABConvertor.Lab>();	

	    private sealed class Pnnbin {
            internal float ac, Lc, Ac, Bc;
            internal int cnt;
            internal int nn, fw, bk, tm, mtm;
            internal double err;
	    }
	
	    private void getLab(int argb, out CIELABConvertor.Lab lab1)
	    {
            Color c = Color.FromArgb(argb);
		    if (!pixelMap.TryGetValue(argb, out lab1)) {
			    lab1 = CIELABConvertor.RGB2LAB(c);
			    pixelMap[argb] = lab1;
		    }
	    }

	    private void find_nn(Pnnbin[] bins, int idx)
	    {
		    int nn = 0;
		    double err = 1e100;

		    var bin1 = bins[idx];
		    int n1 = bin1.cnt;
		    double wa = bin1.ac;
		    double wL = bin1.Lc;
		    double wA = bin1.Ac;
		    double wB = bin1.Bc;
		    for (int i = bin1.fw; i != 0; i = bins[i].fw) {
			    double nerr = sqr(bins[i].ac - wa) + sqr(bins[i].Lc - wL) + sqr(bins[i].Ac - wA) + sqr(bins[i].Bc - wB);
			    double n2 = bins[i].cnt;
			    nerr *= (n1 * n2) / (n1 + n2);
			    if (nerr >= err)
				    continue;
			    err = nerr;
			    nn = i;
		    }
		    bin1.err = err;
		    bin1.nn = nn;
	    }

	    private int pnnquan(int[] pixels, ColorPalette palette, int nMaxColors)
	    {
            var bins = new Pnnbin[65536];            		    

		    /* Build histogram */
		    for(int i=0; i<pixels.Length; ++i) {
			    // !!! Can throw gamma correction in here, but what to do about perceptual
			    // !!! nonuniformity then?
                Color c = Color.FromArgb(pixels[i]);
                int index = getARGBIndex(pixels[i]);
                CIELABConvertor.Lab lab1;
                getLab(pixels[i], out lab1);
			    if(bins[index] == null)
				    bins[index] = new Pnnbin();
                bins[index].ac += c.A;
                bins[index].Lc += lab1.L;
                bins[index].Ac += lab1.A;
                bins[index].Bc += lab1.B;
                bins[index].cnt++;
		    }

		    /* Cluster nonempty bins at one end of array */
		    int maxbins = 0;
            var heap = new int[65537];
            for (int i = 0; i < bins.Length; ++i)
            {
			    if (bins[i] == null)
				    continue;

                float d = 1.0f / (float)bins[i].cnt;
			    bins[i].ac *= d;
			    bins[i].Lc *= d;
			    bins[i].Ac *= d;
			    bins[i].Bc *= d;
			    bins[maxbins++] = bins[i];
		    }

		    for (int i = 0; i < maxbins - 1; i++) {
			    bins[i].fw = (i + 1);
			    bins[i + 1].bk = i;
		    }
            // !!! Already zeroed out by calloc()
            //	bins[0].bk = bins[i].fw = 0;

            int h, l, l2;
            /* Initialize nearest neighbors and build heap of them */
            for (int i = 0; i < maxbins; i++) {
			    find_nn(bins, i);
                /* Push slot on heap */
                double err = bins[i].err;
                
			    for (l = ++heap[0]; l > 1; l = l2) {
				    l2 = l >> 1;
				    if (bins[h = heap[l2]].err <= err)
					    break;
				    heap[l] = h;
			    }
			    heap[l] = i;
		    }

		    /* Merge bins which increase error the least */
		    int extbins = maxbins - nMaxColors;
		    for (int i = 0; i < extbins; ) {
                Pnnbin tb = null;                
                /* Use heap to find which bins to merge */
                for (; ; )
                {
                    int b1 = heap[1];
                    tb = bins[b1]; /* One with least error */
                    /* Is stored error up to date? */
                    if ((tb.tm >= tb.mtm) && (bins[tb.nn].mtm <= tb.tm))
                        break;
                    if (tb.mtm == 0xFFFF) /* Deleted node */
                        b1 = heap[1] = heap[heap[0]--];
                    else /* Too old error value */
                    {
                        find_nn(bins, b1);
                        tb.tm = i;
                    }
                    /* Push slot down */
                    double err = bins[b1].err;
                    for (l = 1; (l2 = l + l) <= heap[0]; l = l2)
                    {
                        if ((l2 < heap[0]) && (bins[heap[l2]].err > bins[heap[l2 + 1]].err))
                            l2++;
                        if (err <= bins[h = heap[l2]].err)
                            break;
                        heap[l] = h;
                    }
                    heap[l] = b1;
                }

                /* Do a merge */
			    var nb = bins[tb.nn];
                float n1 = tb.cnt;
                float n2 = nb.cnt;
                float d = 1.0f / (n1 + n2);
			    tb.ac = d * (n1 * tb.ac + n2 * nb.ac);
			    tb.Lc = d * (n1 * tb.Lc + n2 * nb.Lc);
			    tb.Ac = d * (n1 * tb.Ac + n2 * nb.Ac);
			    tb.Bc = d * (n1 * tb.Bc + n2 * nb.Bc);
			    tb.cnt += nb.cnt;
			    tb.mtm = ++i;

			    /* Unchain deleted bin */
			    bins[nb.bk].fw = nb.fw;
			    bins[nb.fw].bk = nb.bk;
			    nb.mtm = 0xFFFF;
		    }
            heap = null;

            /* Fill palette */
            int k = 0;
		    for (int i = 0;; ++k) {
			    CIELABConvertor.Lab lab1;
			    lab1.alpha = (int) Math.Round(bins[i].ac);
			    lab1.L = bins[i].Lc; lab1.A = bins[i].Ac; lab1.B = bins[i].Bc;
			    palette.Entries[k] = CIELABConvertor.LAB2RGB(lab1);
			    if (hasTransparency && palette.Entries[k] == m_transparentColor) {
                    Color temp = palette.Entries[0];
				    palette.Entries[0] = palette.Entries[k];
                    palette.Entries[k] = temp;
                }

			    if ((i = bins[i].fw) == 0)
				    break;
		    }

		    return k;
	    }

        private short nearestColorIndex(ColorPalette palette, int nMaxColors, int argb)
	    {
            short k = 0;
            Color c = Color.FromArgb(argb);

		    double mindist = int.MaxValue;
            CIELABConvertor.Lab lab1;
            getLab(argb, out lab1);

            for (short i = 0; i < nMaxColors; ++i)
            {
                Color c2 = palette.Entries[i];
                CIELABConvertor.Lab lab2;
                getLab(c2.ToArgb(), out lab2);

                double curdist = sqr(c2.A - c.A);
			    if (curdist > mindist)
				    continue;

                if (nMaxColors < 256) {
				    double deltaL_prime_div_k_L_S_L = CIELABConvertor.L_prime_div_k_L_S_L(lab1, lab2);
                    curdist += sqr(deltaL_prime_div_k_L_S_L);
				    if (curdist > mindist)
					    continue;

				    double a1Prime, a2Prime, CPrime1, CPrime2;
				    double deltaC_prime_div_k_L_S_L = CIELABConvertor.C_prime_div_k_L_S_L(lab1, lab2, out a1Prime, out a2Prime, out CPrime1, out CPrime2);
                    curdist += sqr(deltaC_prime_div_k_L_S_L);
				    if (curdist > mindist)
					    continue;

				    double barCPrime, barhPrime;
				    double deltaH_prime_div_k_L_S_L = CIELABConvertor.H_prime_div_k_L_S_L(lab1, lab2, a1Prime, a2Prime, CPrime1, CPrime2, out barCPrime, out barhPrime);
                    curdist += sqr(deltaH_prime_div_k_L_S_L);
				    if (curdist > mindist)
					    continue;

				    curdist += CIELABConvertor.R_T(barCPrime, barhPrime, deltaC_prime_div_k_L_S_L, deltaH_prime_div_k_L_S_L);
				    if (curdist > mindist)
					    continue;
			    }
			    else {
                    curdist += sqr(lab2.L - lab1.L);
				    if (curdist > mindist)
					    continue;

                    curdist += sqr(lab2.A - lab1.A);
				    if (curdist > mindist)
					    continue;

                    curdist += sqr(lab2.B - lab1.B);
				    if (curdist > mindist)
					    continue;
			    }

			    mindist = curdist;
			    k = i;
		    }
		    return k;
	    }

        private short closestColorIndex(ColorPalette palette, int nMaxColors, int pixel)
	    {
            short k = 0;
            ushort[] closest;
            Color c = Color.FromArgb(pixel);
            if (!closestMap.TryGetValue(pixel, out closest))
            {
                closest = new ushort[5];
                closest[2] = closest[3] = ushort.MaxValue;
                CIELABConvertor.Lab lab1;
                getLab(pixel, out lab1);

                for (; k < nMaxColors; k++)
                {
                    Color c2 = palette.Entries[k];
                    CIELABConvertor.Lab lab2;
                    getLab(c2.ToArgb(), out lab2);

                    closest[4] = (ushort)(sqr(lab2.alpha - lab1.alpha) + CIELABConvertor.CIEDE2000(lab2, lab1));
                    //closest[4] = (short) (Math.abs(lab2.alpha - lab1.alpha) + Math.abs(lab2.L - lab1.L) + Math.abs(lab2.A - lab1.A) + Math.abs(lab2.B - lab1.B));
				if (closest[4] < closest[2]) {
					closest[1] = closest[0];
					closest[3] = closest[2];
                    closest[0] = (ushort)k;
					closest[2] = closest[4];
				}
				else if (closest[4] < closest[3]) {
                    closest[1] = (ushort)k;
					closest[3] = closest[4];
				}
			}

                if (closest[3] == ushort.MaxValue)
                    closest[2] = 0;
            }
            
            if (closest[2] == 0 || (rand.Next(short.MaxValue) % (closest[3] + closest[2])) <= closest[3])
                k = (short) closest[0];
            else
                k = (short) closest[1];

            closestMap[pixel] = closest;
            return k;
	    }

        private bool quantize_image(int[] pixels, ColorPalette palette, int nMaxColors, short[] qPixels, int width, int height, bool dither)
        {
            int pixelIndex = 0;
            if (dither)
            {                                
                const int DJ = 4;
                const int DITHER_MAX = 20;
                int err_len = (width + 2) * DJ;
                int[] clamp = new int[DJ * 256];
                int[] limtb = new int[512];                

                for (int i = 0; i < 256; ++i)
                {
                    clamp[i] = 0;
                    clamp[i + 256] = i;
                    clamp[i + 512] = Byte.MaxValue;
                    clamp[i + 768] = Byte.MaxValue;

                    limtb[i] = -DITHER_MAX;
                    limtb[i + 256] = DITHER_MAX;
                }
                for (int i = -DITHER_MAX; i <= DITHER_MAX; i++)
                    limtb[i + 256] = i;

                bool odd_scanline = false;
                var erowerr = new short[err_len];
                var orowerr = new short[err_len];
                var lookup = new short[65536];
                for (int i = 0; i < height; ++i)
                {
                    int dir;
                    short[] row0, row1;
                    if (odd_scanline)
                    {
                        dir = -1;
                        pixelIndex += (width - 1);
                        row0 = orowerr;
                        row1 = erowerr;
                    }
                    else
                    {
                        dir = 1;
                        row0 = erowerr;
                        row1 = orowerr;
                    }

                    int cursor0 = DJ, cursor1 = width * DJ;
                    row1[cursor1] = row1[cursor1 + 1] = row1[cursor1 + 2] = row1[cursor1 + 3] = 0;
                    for (int j = 0; j < width; ++j)
                    {
                        Color c = Color.FromArgb(pixels[pixelIndex]);
                        int r_pix = clamp[((row0[cursor0] + 0x1008) >> 4) + c.R];
                        int g_pix = clamp[((row0[cursor0 + 1] + 0x1008) >> 4) + c.G];
                        int b_pix = clamp[((row0[cursor0 + 2] + 0x1008) >> 4) + c.B];
                        int a_pix = clamp[((row0[cursor0 + 3] + 0x1008) >> 4) + c.A];

                        Color c1 = Color.FromArgb(a_pix, r_pix, g_pix, b_pix);
                        int offset = getARGBIndex(c1.ToArgb());
                        if (lookup[offset] == 0)
                            lookup[offset] = (short) (nearestColorIndex(palette, nMaxColors, c1.ToArgb()) + 1);
                        qPixels[pixelIndex] = (short)(lookup[offset] - 1);

                        Color c2 = palette.Entries[qPixels[pixelIndex]];
                        if (c2.A < Byte.MaxValue && c.A == Byte.MaxValue)
                        {
                            lookup[offset] = (short) (nearestColorIndex(palette, nMaxColors, pixels[pixelIndex]) + 1);
                            qPixels[pixelIndex] = (short)(lookup[offset] - 1);
                        }

                        r_pix = limtb[r_pix - c2.R + 256];
                        g_pix = limtb[g_pix - c2.G + 256];
                        b_pix = limtb[b_pix - c2.B + 256];
                        a_pix = limtb[a_pix - c2.A + 256];

                        int k = r_pix * 2;
                        row1[cursor1 - DJ] = (short) r_pix;
                        row1[cursor1 + DJ] += (short) (r_pix += k);
                        row1[cursor1] += (short) (r_pix += k);
                        row0[cursor0 + DJ] += (short)(r_pix += k);

                        k = g_pix * 2;
                        row1[cursor1 + 1 - DJ] = (short) g_pix;
                        row1[cursor1 + 1 + DJ] += (short) (g_pix += k);
                        row1[cursor1 + 1] += (short) (g_pix += k);
                        row0[cursor0 + 1 + DJ] += (short) (g_pix += k);

                        k = b_pix * 2;
                        row1[cursor1 + 2 - DJ] = (short) b_pix;
                        row1[cursor1 + 2 + DJ] += (short) (b_pix += k);
                        row1[cursor1 + 2] += (short) (b_pix += k);
                        row0[cursor0 + 2 + DJ] += (short) (b_pix += k);

                        k = a_pix * 2;
                        row1[cursor1 + 3 - DJ] = (short) a_pix;
                        row1[cursor1 + 3 + DJ] += (short) (a_pix += k);
                        row1[cursor1 + 3] += (short) (a_pix += k);
                        row0[cursor0 + 3 + DJ] += (short) (a_pix += k);

                        cursor0 += DJ;
                        cursor1 -= DJ;
                        pixelIndex += dir;
                    }
                    if ((i % 2) == 1)
                        pixelIndex += width + 1;

                    odd_scanline = !odd_scanline;
                }
                return true;
            }

            if (hasTransparency || nMaxColors < 64)
            {
                for (int i = 0; i < qPixels.Length; i++)
                    qPixels[i] = nearestColorIndex(palette, nMaxColors, pixels[i]);
            }
            else
            {
                for (int i = 0; i < qPixels.Length; i++)
                    qPixels[i] = closestColorIndex(palette, nMaxColors, pixels[i]);
            }

            return true;
        }

        public override bool QuantizeImage(Bitmap source, Bitmap dest, int nMaxColors, bool dither)
        {
            int bitDepth = Image.GetPixelFormatSize(source.PixelFormat);
            if (!IsValidFormat(dest.PixelFormat, nMaxColors))
                return false;

            int bitmapWidth = source.Width;
            int bitmapHeight = source.Height;

            hasTransparency = hasSemiTransparency = false;
            int pixelIndex = 0;
            var pixels = new int[bitmapWidth * bitmapHeight];
            if (bitDepth <= 16)
            {
                for (int y = 0; y < bitmapHeight; y++)
                {
                    for (int x = 0; x < bitmapWidth; x++)
                    {
                        Color color = source.GetPixel(x, y);
                        if (color.A < Byte.MaxValue)
                        {
                            hasSemiTransparency = true;
                            if (color.A == 0)
                            {
                                hasTransparency = true;
                                m_transparentColor = color;
                            }
                        }
                        pixels[pixelIndex++] = color.ToArgb();
                    }
                }
            }

            // Lock bits on 3x8 source bitmap
            else
            {
                BitmapData data = source.LockBits(new Rectangle(0, 0, bitmapWidth, bitmapHeight), ImageLockMode.ReadOnly, source.PixelFormat);
                int strideSource;

                unsafe
                {
                    var pRowSource = (byte*)data.Scan0;

                    // Compensate for possible negative stride
                    if (data.Stride > 0)
                        strideSource = data.Stride;
                    else
                    {
                        pRowSource += bitmapHeight * data.Stride;
                        strideSource = -data.Stride;
                    }

                    // First loop: gather color information
                    for (uint y = 0; y < bitmapHeight; y++)
                    {
                        var pPixelSource = pRowSource;
                        // For each row...
                        for (uint x = 0; x < bitmapWidth; x++)
                        {	// ...for each pixel...
                            byte pixelBlue = *pPixelSource++;
                            byte pixelGreen = *pPixelSource++;
                            byte pixelRed = *pPixelSource++;
                            byte pixelAlpha = bitDepth < 32 ? Byte.MaxValue : *pPixelSource++;

                            var c = Color.FromArgb(pixelAlpha, pixelRed, pixelGreen, pixelBlue);
                            if (pixelAlpha < Byte.MaxValue)
                            {
                                hasSemiTransparency = true;
                                if (pixelAlpha == 0)
                                {
                                    hasTransparency = true;
                                    m_transparentColor = c;
                                }
                            }
                            pixels[pixelIndex++] = c.ToArgb();
                        }
                        pRowSource += strideSource;
                    }
                }

                source.UnlockBits(data);
            }

            var qPixels = new short[bitmapWidth * bitmapHeight];
            if (nMaxColors > 256)
            {
                hasSemiTransparency = false;
                quantize_image(pixels, qPixels, bitmapWidth, bitmapHeight);
                return ProcessImagePixels(dest, qPixels);
            }
                        
            var palette = dest.Palette;
            if (nMaxColors > 2)
                nMaxColors = pnnquan(pixels, palette, nMaxColors);
            else
            {
                if (hasTransparency)
                {
                    palette.Entries[0] = Color.Transparent;
                    palette.Entries[1] = Color.Black;
                }
                else
                {
                    palette.Entries[0] = Color.Black;
                    palette.Entries[1] = Color.White;
                }
            }

            quantize_image(pixels, palette, nMaxColors, qPixels, bitmapWidth, bitmapHeight, dither);
            pixelMap.Clear();
            closestMap.Clear();

            return ProcessImagePixels(dest, palette, qPixels);
        }

    }
}
