namespace NL
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable]
    public class CustomEvent
    {
        public string name;
        public UnityEvent OnEventTriggered;
        public UnityAction onEventTriggered;
    }

    public class EventsHandler : MonoBehaviour
    {
        public CustomEvent[] events;

        public void TriggerEvent(int eventID)
        {
            events[eventID].OnEventTriggered?.Invoke();
            events[eventID].onEventTriggered?.Invoke();
        }
    }
}
