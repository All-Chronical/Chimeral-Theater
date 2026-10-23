using UnityEngine;

namespace ChimeralTheater.Films
{
    /// <summary>
    /// Plays a single act back through its stand-in: holds a private copy of the act's frames,
    /// advances a shared session clock each tick and samples the act at that time, hiding the
    /// stand-in once the act has run past its end.
    /// </summary>
    public class ActPlayback
    {
        public readonly Act Act;
        public readonly SphereAvatar Avatar;

        /// <summary>Time into the take's playback session, shared by every act.</summary>
        public float SessionTime { get; private set; }

        public bool HasEnded => SessionTime > Act.Duration;

        public ActPlayback(Act act)
        {
            Act = act;
            Avatar = new SphereAvatar(act.Number);
        }

        /// <summary>Advances to the shared session time. The stand-in is hidden once the act has
        /// run past its end; a session still running with earlier acts keeps going.</summary>
        public void Evaluate(float sessionTime)
        {
            SessionTime = sessionTime;

            if (HasEnded)
            {
                Avatar.Hide();
                return;
            }

            // TrySample returns false while the frames are unloaded, which only happens
            // after the act's file is gone; treat that as ended rather than posing at zero.
            if (Act.TrySample(sessionTime, out ActFrame frame))
                Avatar.Apply(frame);
            else
                Avatar.Hide();
        }

        public void Dispose()
        {
            Avatar.Destroy();
        }
    }
}
