using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Threading;
using SharpDX.DirectInput;
using MouseState = Microsoft.Xna.Framework.Input.MouseState;
using KeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;
using Mouse = Microsoft.Xna.Framework.Input.Mouse;
using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;
using Joystick = SharpDX.DirectInput.Joystick;
using JoystickState = SharpDX.DirectInput.JoystickState;

public enum SwitchButton
{
    B = 0,
    A = 1,
    Y,
    X,
    LB,
    RB,
    LT,
    RT,
    Minus,
    Plus,
    LS,
    RS
}

/*
 * Controller enable for generic controllers is currently off!!! 
 * Turn it on manually to test!
 */ 

public class InputHelper
{
    protected MouseState currentMouseState, previousMouseState;
    protected KeyboardState currentKeyboardState, previousKeyboardState;
    protected Vector2 scale, offset;
    protected GamePadState[] currentGamePadState, previousGamePadState;
    protected JoystickState currentJoyState, previousJoyState;
    //protected JoystickCapabilities joystickCapabilities;
    protected DirectInput directInput;
    protected Joystick joystick;
    protected Guid joystickGuid;
    protected float JoystickPingDuration = 5.0f, JoystickPing = 5.0f;
    protected float rumbleDuration, leftMotor, rightMotor;
    protected bool enableControllers;
    
    public InputHelper()
    {
        scale = Vector2.One;
        offset = Vector2.Zero;

        currentGamePadState = new GamePadState[Enum.GetValues(typeof(PlayerIndex)).Length];
        foreach (PlayerIndex index in Enum.GetValues(typeof(PlayerIndex)))
            currentGamePadState[(int)index] = GamePad.GetState(index);

        //currentJoyState = Joystick.GetState(0);
        enableControllers = false;
        directInput = new DirectInput();
    }

    public void Update(GameTime gameTime)
    {
        previousMouseState = currentMouseState;
        previousKeyboardState = currentKeyboardState;
        previousGamePadState = (GamePadState[])currentGamePadState.Clone();
        //previousJoyState = currentJoyState;

        currentMouseState = Mouse.GetState();
        currentKeyboardState = Keyboard.GetState();

        foreach(PlayerIndex index in Enum.GetValues(typeof(PlayerIndex)))
            currentGamePadState[(int)index] = GamePad.GetState(index);

        //currentJoyState = Joystick.GetState(0);
        //joystickCapabilities = Joystick.GetCapabilities(0);

        if(RumbleDuration > 0)
        {
            GamePadVibration(PlayerIndex.One, .5f, .5f);
            rumbleDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (!currentGamePadState[0].IsConnected && enableControllers)
        {
            JoystickPing -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (JoystickPing < 0)
            {
                JoystickPing = JoystickPingDuration;
                var th = new Thread(GenericControllerConnection);
                th.Start();
                
                //Console.WriteLine("A new thread has been created!");
            }
        }
    }

    protected void GenericControllerConnection()
    {
        //Console.WriteLine("Launched new thread!");
        if (joystick == null)
        {
            //Console.WriteLine("Looking for Joystick!");

            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            //If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                    foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                        joystickGuid = deviceInstance.InstanceGuid;

            if (joystickGuid == Guid.Empty)
            {
                return;
            }

            //Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);


            joystick = new Joystick(directInput, joystickGuid);
            var allEffects = joystick.GetEffects();
            //foreach (var effectInfo in allEffects)
            //    Console.WriteLine("Effect available {0}", effectInfo.Name);
            joystick.Properties.BufferSize = 128;
            joystick.Acquire();
        }
        else { 
            joystick.Poll();
            //Console.WriteLine("Polling instead...");

            try
            {
                JoystickState state = joystick.GetCurrentState();
                bool[] button = state.Buttons;
                int[] buttons = state.PointOfViewControllers;
            }
            catch (Exception)
            {
                //Console.WriteLine("Oops, the controller disconnected!");
                joystick = null;
            }
        }
    }

    public float RumbleDuration
    {
        get { return rumbleDuration; }
        set { rumbleDuration = value; }
    }

    public Vector2 Scale
    {
        get { return scale; }
        set { scale = value; }
    }

    public Vector2 Offset
    {
        get { return offset; }
        set { offset = value; }
    }

    public Vector2 MousePosition
    {
        get { return (new Vector2(currentMouseState.X, currentMouseState.Y) - offset) / scale; }
    }

    public bool MouseLeftButtonPressed()
    {
        return currentMouseState.LeftButton ==
            ButtonState.Pressed && 
            previousMouseState.LeftButton
            == ButtonState.Released;
    }

    public bool MouseLeftButtonDown()
    {
        return currentMouseState.LeftButton ==
            ButtonState.Pressed;
    }

    

    // vibration methods for Xbox 360 controller
    // clears vibration set in controller index
    public void ClearVibration(PlayerIndex index)
    {
        GamePadVibration(index, 0f, 0f);
    }

    // sets controller vibration in controller index
    public void GamePadVibration(PlayerIndex index, float leftMotor, float rightMotor, float duration = 1.0f)
    {
        this.leftMotor = leftMotor;
        this.rightMotor = rightMotor;
        rumbleDuration = duration;
    }

    public bool ButtonReleased(Buttons button, PlayerIndex index)
    {
        return currentGamePadState[(int)index].IsButtonUp(button) &&
            previousGamePadState[(int)index].IsButtonDown(button);
    }

    public bool ButtonPressed(Buttons button, PlayerIndex index)
    {
        return currentGamePadState[(int)index].IsButtonDown(button) &&
            previousGamePadState[(int)index].IsButtonUp(button);
    }

    public bool IsButtonDown(Buttons button, PlayerIndex index)
    {
        return currentGamePadState[(int)index].IsButtonDown(button);
    }    

    public bool KeyPressed(Keys k)
    {
        return currentKeyboardState.IsKeyDown(k) &&
            previousKeyboardState.IsKeyUp(k);
    }

    public bool IsKeyDown(Keys k)
    {
        return currentKeyboardState.IsKeyDown(k);
    }

    public bool AnyKeyPressed
    {
        get {
            return currentKeyboardState.GetPressedKeys().Length
              > 0 &&
              previousKeyboardState.GetPressedKeys().Length == 0;
        }
    }
}

// Generic button press using enums
//public bool GenButtonPressed(SwitchButton button)
//{
//    if (!joystickCapabilities.IsConnected)
//        return false;
//    if (currentJoyState.Buttons[(int)button] == ButtonState.Pressed
//        && previousJoyState.Buttons[(int)button] == ButtonState.Released)
//        return true;
//    return false;
//}

//// Generic button release using enums
//public bool GenButtonReleased(SwitchButton button)
//{
//    if (!joystickCapabilities.IsConnected)
//        return false;
//    if (currentJoyState.Buttons[(int)button] == ButtonState.Released
//        && previousJoyState.Buttons[(int)button] == ButtonState.Pressed)
//        return true;
//    return false;
//}

//// generic button release using index
//public bool GenButtonReleased(int index)
//{
//    if (!joystickCapabilities.IsConnected)
//        return false;
//    if (currentJoyState.Buttons[index] == ButtonState.Released
//        && previousJoyState.Buttons[index] == ButtonState.Pressed)
//        return true;
//    return false;
//}

//// generic button press using index
//public bool GenButtonPressed(int index)
//{
//    if (!joystickCapabilities.IsConnected)
//        return false;
//    if (currentJoyState.Buttons[index] == ButtonState.Pressed
//        && previousJoyState.Buttons[index] == ButtonState.Released)
//        return true;
//    return false;
//}

//// generic hat press using index
//public bool GenHatPressed(char index)
//{
//    if (!joystickCapabilities.IsConnected)
//        return false;
//    JoystickHat[] joystickHat = currentJoyState.Hats;
//    switch (index)
//    {
//        case 'd':
//            {
//                if (currentJoyState.Hats[0].Down == ButtonState.Pressed &&
//                        previousJoyState.Hats[0].Down == ButtonState.Released)
//                    return true;
//            }
//            break;
//        case 'u':
//            {
//                if (currentJoyState.Hats[0].Up == ButtonState.Pressed &&
//                        previousJoyState.Hats[0].Up == ButtonState.Released)
//                    return true;
//            }
//            break;
//        case 'l':
//            {
//                if (currentJoyState.Hats[0].Left == ButtonState.Pressed &&
//                        previousJoyState.Hats[0].Left == ButtonState.Released)
//                    return true;
//            }
//            break;
//        case 'r':
//            {
//                if (currentJoyState.Hats[0].Right == ButtonState.Pressed &&
//                        previousJoyState.Hats[0].Right == ButtonState.Released)
//                    return true;
//            }
//            break;
//    }
//    return false;
//}

// generic hat press using index
//public bool IsGenHatPressed(char index)
//{
//    if (!joystickCapabilities.IsConnected)
//        return false;
//    switch (index)
//    {
//        case 'd':
//            {
//                if (currentJoyState.Hats[0].Down == ButtonState.Pressed)
//                    return true;
//            }
//            break;
//        case 'u':
//            {
//                if (currentJoyState.Hats[0].Up == ButtonState.Pressed)
//                    return true;
//            }
//            break;
//        case 'l':
//            {
//                if (currentJoyState.Hats[0].Left == ButtonState.Pressed)
//                    return true;
//            }
//            break;
//        case 'r':
//            {
//                if (currentJoyState.Hats[0].Right == ButtonState.Pressed)
//                    return true;
//            }
//            break;
//    }
//    return false;
//}