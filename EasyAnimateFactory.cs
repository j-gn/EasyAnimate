using UnityEngine;
using System;
public static class EasyAnimateFactory
{
    /// <param name="pStartValue"></param>
    /// <param name="pEndValue"></param>
    /// <param name="pSpeed">uints per second</param>
    /// <param name="pSetter"></param>
    /// <returns></returns>
    public static EasyAnimate.State<float> FloatWithSpeed(float pStartValue, float pEndValue, float pSpeed, EasyAnimate.State<float>.EAInterpolate pInterpolator = null, EasyAnimate.State<float>.EASetter pSetter = null, Action pOnComplete = null) {
        return new EasyAnimate.State<float>() {
            startTime = Time.time,
            length = Mathf.Abs(pEndValue - pStartValue) / pSpeed,
            startValue = pStartValue,
            endValue = pEndValue,
            onCompleted = pOnComplete,
            interpolator = Mathf.Lerp,
            setter = pSetter,
        };
    }
}