using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace BaseFunction
{
    public abstract class BaseHookClass : IDisposable
    {
        public BaseHookClass()
        {           
        }
        public BaseHookClass(List<int> keys)
        { 
            Keys.AddRange(keys);
        }
        public BaseHookClass(int key)
        {
            Keys.Add(key);
        }
        public BaseHookClass(System.Windows.Forms.Keys key)
        {
            Keys.Add((int)key);
        }

        [DllImport("accore.dll",
            CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?acedRegisterFilterWinMsg@@YA_NQ6A_NPEAUtagMSG@@@Z@Z")] //с версии AutoCAD2019
        private static extern bool acedRegisterFilterWinMsg(WindowHookProc callBackFunc);


        [DllImport("user32.dll")]
        public static extern int GetKeyboardState(byte[] keystate);


        [DllImport("accore.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?acedRemoveFilterWinMsg@@YA_NQ6A_NPEAUtagMSG@@@Z@Z")]
        private static extern bool acedRemoveFilterWinMsg(WindowHookProc callBackFunc);

        const int WM_KEYDOWN = 0x100; // Нажатие клавиши
        const int WM_KEYUP = 0x101; // Отжатие клавиши

        // Функция обратного вызова для хука
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool WindowHookProc(ref System.Windows.Forms.Message msg);

        private bool Hook(ref System.Windows.Forms.Message msg)
        {
            // проверяем структуру msg на то, что нас интересует,
            // например, клавиши, движения мыши, и т.д.

            if (msg.Msg == WM_KEYDOWN)
            {
                byte[] keys = new byte[256];

                GetKeyboardState(keys);

                int value = 128;
                foreach (var key in Keys) value &= keys[(int)key];
                
                if (value == 128)
                {
                    this.Action();
                }
            }
            return false;
        }

        public virtual void Action()
        { 
            
        }

        private static WindowHookProc callBackFunc = null;
               
        public void Register()
        {
            if (callBackFunc != null)
            {
                acedRemoveFilterWinMsg(callBackFunc);
            }
            callBackFunc = new WindowHookProc(Hook);
            acedRegisterFilterWinMsg(callBackFunc);
        }

        public void UnRegister()
        {
            if (callBackFunc != null)
            {
                acedRemoveFilterWinMsg(callBackFunc);
                callBackFunc = null;
            }
        }

        public void Dispose()
        {            
            UnRegister();
        }      

        public List<int> Keys = new List<int>();
    }
}
