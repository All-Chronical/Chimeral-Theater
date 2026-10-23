using System.Collections.Generic;
using UniRes;

namespace ChimeralTheater.Films
{
    /// <summary>
    /// A film: a numbered collection of acts. Acts are played back together, each starting at
    /// time zero. A take lives in memory; each act is stored in its own file (see <see cref="ActStore"/>).
    /// </summary>
    public class Take : Resource
    {
        [Export] public int Number;

        /// <summary>The number the next added act receives. Numbers are never reused within a take.</summary>
        [Export] public int NextActNumber = 1;

        [Export] public List<Act> Acts = new List<Act>();

        /// <summary>True when at least one act has a recording.</summary>
        public bool HasRecordedActs
        {
            get
            {
                foreach (Act act in Acts)
                {
                    if (!act.IsEmpty)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Adds an empty act with the next number.</summary>
        public Act AddAct()
        {
            var act = new Act { Number = NextActNumber++ };
            act.ResourceName = $"Act {act.Number}";
            Acts.Add(act);
            return act;
        }
    }
}
