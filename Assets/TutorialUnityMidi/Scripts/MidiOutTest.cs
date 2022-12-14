using UnityEngine;
using System.Collections.Generic;
using TutorialUnityMidi.RtMidiWrapper;

namespace TutorialUnityMidi
{
    public sealed class MidiOutTest : MonoBehaviour
    {
        private MidiProbe _probe;
        private List<MidiOutPort> _ports = new ();

        private void Awake()
        {
            _probe = new MidiProbe(MidiProbe.Mode.Out);
        }

        private void Start()
        {
            // Send an all-sound-off message.
            foreach (var port in _ports) port?.SendAllOff(0);
            _ports[0]?.SendNoteOn(0, 40, 100);
        }

        private void Update()
        {
            // Rescan when the number of ports changed.
            if (_ports.Count == _probe.PortCount) return;
            
            DisposePorts();
            ScanPorts();
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
                Debug.Log("MIDI-out port found: " + nm);
                _ports.Add(IsRealPort(nm) ? new MidiOutPort(i) : null);
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