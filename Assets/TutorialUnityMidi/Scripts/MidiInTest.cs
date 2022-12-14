using UnityEngine;
using System.Collections.Generic;
using TutorialUnityMidi.RtMidiWrapper;

namespace TutorialUnityMidi
{
    public sealed class MidiInTest : MonoBehaviour
    {
        private MidiProbe _probe;
        private List<MidiInPort> _ports = new ();

        private void Awake()
        {
            _probe = new MidiProbe(MidiProbe.Mode.In);
        }

        private void Update()
        {
            // Rescan when the number of ports changed.
            if (_ports.Count != _probe.PortCount)
            {
                DisposePorts();
                ScanPorts();
            }

            // Process queued messages in the opened ports.
            foreach (var p in _ports) p?.ProcessMessages();
        }

        private void OnDestroy()
        {
            _probe?.Dispose();
            DisposePorts();
        }
        
        // Does the port seem real or not?
        // This is mainly used on Linux (ALSA) to filter automatically generated
        // virtual ports.
        private bool IsRealPort(string nm) => !nm.Contains("Through") && !nm.Contains("RtMidi");

        // Scan and open all the available output ports.
        private void ScanPorts()
        {
            for (var i = 0; i < _probe.PortCount; i++)
            {
                var nm = _probe.GetPortName(i);
                Debug.Log("MIDI-in port found: " + nm);

                _ports.Add(IsRealPort(nm) ? new MidiInPort(i)
                    {
                        OnNoteOn = (byte channel, byte note, byte velocity) =>
                            Debug.Log($"{nm} [{channel}] On {note} ({velocity})"),

                        OnNoteOff = (byte channel, byte note) =>
                            Debug.Log($"{nm} [{channel}] Off {note})"),

                        OnControlChange = (byte channel, byte number, byte value) =>
                            Debug.Log($"{nm} [{channel}] CC {number} ({value})")
                    } : null
                );
            }
        }

        // Close and release all the opened ports.
        private void DisposePorts()
        {
            foreach (var p in _ports) p?.Dispose();
            _ports.Clear();
        }
    }
}
