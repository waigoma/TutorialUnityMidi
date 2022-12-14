using RtMidiDll = RtMidi.Unmanaged;

namespace TutorialUnityMidi.RtMidiWrapper
{
    public sealed unsafe class MidiInPort : System.IDisposable
    {
        public System.Action<byte, byte, byte> OnNoteOn;
        public System.Action<byte, byte> OnNoteOff;
        public System.Action<byte, byte, byte> OnControlChange;
        
        private RtMidiDll.Wrapper* _rtmidi;

        public MidiInPort(int portNumber)
        {
            _rtmidi = RtMidiDll.InCreateDefault();

            if (_rtmidi != null && _rtmidi->ok)
                RtMidiDll.OpenPort(_rtmidi, (uint)portNumber, "RtMidi In");

            if (_rtmidi == null || !_rtmidi->ok)
                throw new System.InvalidOperationException("Failed to set up a MIDI input port.");
        }

        ~MidiInPort()
        {
            if (_rtmidi == null || !_rtmidi->ok) return;

            RtMidiDll.InFree(_rtmidi);
        }

        public void Dispose()
        {
            if (_rtmidi == null || !_rtmidi->ok) return;

            RtMidiDll.InFree(_rtmidi);
            _rtmidi = null;

            System.GC.SuppressFinalize(this);
        }

        public void ProcessMessages()
        {
            if (_rtmidi == null || !_rtmidi->ok) return;

            var msg = stackalloc byte [32];

            while (true)
            {
                ulong size = 32;
                var stamp = RtMidiDll.InGetMessage(_rtmidi, msg, ref size);
                if (stamp < 0 || size == 0) break;

                var status = (byte)(msg[0] >> 4);
                var channel = (byte)(msg[0] & 0xf);

                switch (status)
                {
                    case 9:
                        if (msg[2] > 0)
                            OnNoteOn?.Invoke(channel, msg[1], msg[2]);
                        else
                            OnNoteOff?.Invoke(channel, msg[1]);
                        break;
                    case 8:
                        OnNoteOff?.Invoke(channel, msg[1]);
                        break;
                    case 0xb:
                        OnControlChange?.Invoke(channel, msg[1], msg[2]);
                        break;
                }
            }
        }
    }
}