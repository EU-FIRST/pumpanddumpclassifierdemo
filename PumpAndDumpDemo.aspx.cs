/*==========================================================================;
 *
 *  This file is part of the FIRST open-source software. 
 *  See http://project-first.eu/
 *
 *  File:    TwitterSentimentDemo.aspx.cs
 *  Desc:    Twitter sentiment demo request handler
 *  Created: Sep-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Web.UI;
using System.Threading;
using System.Web;
using System.IO;
using Latino;
using Latino.Model;
using Latino.TextMining;

public partial class TwitterSentimentDemo : Page 
{
    private static SvmBinaryClassifier<int> mClassifier;
    private static BowSpace mBowSpace;
    private static bool mReady
        = false;

    static TwitterSentimentDemo()
    {
        string modelFileName = HttpContext.Current.Server.MapPath("App_Data\\AdCfy.bin");
        string bowSpcFileName = HttpContext.Current.Server.MapPath("App_Data\\AdCfyBowSpc.bin");
        mClassifier = new SvmBinaryClassifier<int>();
        mClassifier.LoadModel(modelFileName);
        BinarySerializer bs = new BinarySerializer(bowSpcFileName, FileMode.Open);
        mBowSpace = new BowSpace(bs);
        bs.Close();
        mReady = true;
    }

    public static string JsEncode(string text)
    {
        string jsTxt = "";
        foreach (char ch in text)
        {
            if (ch >= 32 && ch <= 126 && ch != '"' && ch != '\'' && ch != '\\') { jsTxt += ch; }
            else { jsTxt += "\\u" + ((int)ch).ToString("X").PadLeft(4, '0'); }
        }
        return jsTxt;
    }

    static void ExplainToString(ArrayList<KeyDat<double, int>> expl, out string wPos, out string wNeg)
    {
        wPos = "";
        wNeg = "";
        expl.Sort(DescSort<KeyDat<double, int>>.Instance);
        foreach (KeyDat<double, int> word in expl)
        {
            if (word.First > 0 && word.Second != -1)
            {
                wPos += string.Format("{0} ({1:0.00}), ", (word.Second == -1 ? "(bias)" : mBowSpace.Words[word.Second].MostFrequentForm), word.First);
            }
        }
        expl.Sort();
        foreach (KeyDat<double, int> word in expl)
        {
            if (word.First < 0 && word.Second != -1)
            {
                wNeg += string.Format("{0} ({1:0.00}), ", (word.Second == -1 ? "(bias)" : mBowSpace.Words[word.Second].MostFrequentForm), word.First);
            }
        }
        wPos = wPos.TrimEnd(',', ' ');
        wNeg = wNeg.TrimEnd(',', ' ');
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        string text = Request.Params["text"];
        if (!string.IsNullOrEmpty(text))
        {
            while (!mReady) { Thread.Sleep(1000); }
            SparseVector<double> bow = mBowSpace.ProcessDocument(text);
            Prediction<int> p = mClassifier.Predict(bow);
            ArrayList<KeyDat<double, int>> expl = mClassifier.Explain(bow);
            string wPos, wNeg;
            ExplainToString(expl, out wPos, out wNeg);
            Response.Write("{p:" + p.BestClassLabel.ToString() + ",wpos:\"" + JsEncode(wPos) + "\",wneg:\"" + JsEncode(wNeg) + "\"}");
        }
    }
}