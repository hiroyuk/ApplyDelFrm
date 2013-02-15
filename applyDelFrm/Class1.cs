using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using SAPStudio.AvsFilterNet;

[assembly: AvisynthFilterClass(typeof(ApplyDelFrm.ApplyDelFrm), "ApplyDelFrm", "cs")]
[assembly: AvisynthFilterClass(typeof(ApplyDelFrm.LocalDeleteFrame), "LocalDeleteFrame", "ci+")]
namespace ApplyDelFrm
{
    public class ApplyDelFrm : LocalDeleteFrame
    {
        public ApplyDelFrm(AVSValue args, ScriptEnvironment env)
            : base(args, env)
        {

        }

        protected override int[] GetSkipFrames(AVSValue args, ScriptEnvironment env)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            string filename = args[1].AsString();
            HashSet<int> f = new HashSet<int>();
            int lineNo = 0;
            string line = "xx";
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNo++;
                        f.Add(int.Parse(line));
                    }
                }
            }
            catch (Exception ex)
            {
                env.ThrowError(string.Format("line: {0:4} | {1} {2}\n", lineNo, ex.Message, line));
            }

            return f.Distinct().OrderBy(key => key).ToArray<int>();
        }
    }


    public class LocalDeleteFrame : AvisynthFilter
    {
        private VideoInfo vi;
        // 飛ばすフレーム番号
        private int[] frames = null;

        public LocalDeleteFrame(AVSValue args, ScriptEnvironment env)
            : base(args, env)
        {
            vi = GetVideoInfo();
            int[] skips = GetSkipFrames(args, env);

            List<int> f = new List<int>();

            int length = vi.num_frames;
            int skipIdx = 0;
            for (int i = 0; i < length; i++)
            {
                if (skips[skipIdx] == i)
                {
                    skipIdx++;
                    if (skips.Length <= skipIdx)
                    {
                        skipIdx = 0;
                    }

                    vi.num_frames--;
                    continue;
                }
                f.Add(i);
            }
            this.frames = f.ToArray();
            SetVideoInfo(ref vi);
        }

        protected virtual int[] GetSkipFrames(AVSValue args, ScriptEnvironment env)
        {
            int n = args[1].ArraySize();
            HashSet<int> f = new HashSet<int>();
            int max = vi.num_frames;
            for (int i = 0; i < n; ++i)
            {
                int skipFrm = args[1][i].AsInt();
                if (vi.num_frames > skipFrm)
                {
                    f.Add(skipFrm);
                }
            }

            return f.Distinct().OrderBy(key => key).ToArray<int>();
        }


        private int GetCacheSkipCount(int i)
        {
            return frames[i];
        }

        public override VideoFrame GetFrame(int n, ScriptEnvironment env)
        {
            int nn = GetCacheSkipCount(n);
            return Child.GetFrame(nn, env);
        }

        public override bool GetParity(int n)
        {
            int nn = GetCacheSkipCount(n);
            return Child.GetParity(nn);
        }
    }
}
