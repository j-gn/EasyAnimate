using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EasyAnimate
{
    static Dictionary<int, IState> _animStates = new Dictionary<int, IState>();
    public interface IState {
        //returns true as long as object wants to exist
        bool Update(float pCurrentTime);
    }
    
    public struct State<T> : IState {
        public delegate void EASetter(T pValue);
        public delegate T EAInterpolate(T pFrom, T pTo, float pQuota);
        public T startValue;
        public T endValue; 
        public float startTime;
        public float length; //length of animation in seconds
        public Action onCompleted; //action done on complete, does not fire if dissmissed or cleared
        public EAInterpolate interpolator; //function used to interpolate a value over time
        public EASetter setter; //function used to apply the interpolated value    
        /// <summary>
        /// Update is called by the manager
        /// </summary>
        /// <param name="pCurrentTime">current time in seconds</param>
        /// <returns>true if still animating, false if done</returns>
        public bool Update( float pCurrentTime ){
            if ((startTime + length) <= pCurrentTime) {
                if (setter != null) { setter(endValue); }
                if (onCompleted != null) { onCompleted(); }
                return false;
            }
            else {
                if (setter != null) {
                    setter(interpolator(startValue, endValue, (pCurrentTime - startTime) / length));
                }
                return true;
            }

        }
    }

    /// <summary>
    /// Add a new animation state to the manager only if no animation with the same object and channel is not playing
    /// </summary>
    /// <param name="o">an object associated with the animation</param>
    /// <param name="pChannel">a name token</param>
    /// <param name="pState">an EasyAnimate.State object</param>
    public static void SetIfVacant(object o, string pChannel, IState pState) {
        if (!_animStates.ContainsKey(GetHash(o, pChannel)))
            _animStates[GetHash(o, pChannel)] = pState;
    }
    /// <summary>
    /// Add a new animation state to the manager
    /// </summary>
    /// <param name="o">an object associated with the animation</param>
    /// <param name="pChannel">a name token</param>
    /// <param name="pState">an EasyAnimate.State object</param>
    public static void Set(object o, string pChannel, IState pState) {
        _animStates[GetHash(o, pChannel)] =  pState;
    }

    internal static void Update(){
        foreach (KeyValuePair<int, IState> kv in _animStates.ToArray()) {
            int key = kv.Key;
            IState e = kv.Value;
            if(!e.Update(Time.time))
                _animStates.Remove(key);
        }
    }
    /// <summary>
    /// tries to stop an animation
    /// </summary>
    /// <returns>true if an animation was stopped</returns>
    public static bool Stop(object o, string pChannel) { 
        int hash = GetHash(o,pChannel);
        if (_animStates.ContainsKey(hash)) {
            _animStates.Remove(hash);
            return true;
        }
        else {
            return false;
        }

    }

    internal static void Clear() {
        _animStates.Clear();
    }

    private static int GetHash(object o, string pChannel)
    {
        return o.GetHashCode() ^ pChannel.GetHashCode();
    }

}