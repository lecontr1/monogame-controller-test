#define DEBUG

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

public enum SwitchHats
{
    Up = 0,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft
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
    protected bool enableControllers = true;
    
    public InputHelper()
    {
        scale = Vector2.One;
        offset = Vector2.Zero;

        currentGamePadState = new GamePadState[Enum.GetValues(typeof(PlayerIndex)).Length];
        foreach (PlayerIndex index in Enum.GetValues(typeof(PlayerIndex)))
            currentGamePadState[(int)index] = GamePad.GetState(index);

        //currentJoyState = Joystick.GetState(0);        
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

        if(RumbleDuration > 0)
        {
            GamePadVibration(PlayerIndex.One, leftMotor, rightMotor);
            rumbleDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (!currentGamePadState[0].IsConnected && enableControllers && joystick == null)
        {
            JoystickPing -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (JoystickPing < 0)
            {
                JoystickPing = JoystickPingDuration;
                var th = new Thread(GenericControllerConnection);
                th.Start();
#if DEBUG
                Console.WriteLine("A new thread has been created!");
#endif                
            }
        }
        else if (joystick != null && enableControllers)
        {
            joystick.Poll();
#if DEBUG
            Console.WriteLine("Polling Joystick...");
#endif
            try
            {
                JoystickState state = joystick.GetCurrentState();
                currentJoyState = joystick.GetCurrentState();
                bool[] button = state.Buttons;
                int[] hats = state.PointOfViewControllers;
                Console.WriteLine("[{0}]", string.Join(", ", hats));
            }
            catch (Exception)
            {
#if DEBUG
                Console.WriteLine("Oops, the controller disconnected!");
#endif
                joystick = null;
            }
        }
    }

    protected void GenericControllerConnection()
    {
#if DEBUG
        Console.WriteLine("Launched new thread!");
        Console.WriteLine("Looking for Joystick!");
#endif
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
#if DEBUG
        Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);
#endif

        joystick = new Joystick(directInput, joystickGuid);
        var allEffects = joystick.GetEffects();
#if DEBUG
        foreach (var effectInfo in allEffects)
            Console.WriteLine("Effect available {0}", effectInfo.Name);
#endif
        joystick.Properties.BufferSize = 128;
        joystick.Acquire();
       
    }

    public bool GenButtonPressed(Buttons button)
    {
        return currentJoyState.Buttons[(int)button] &&
            !previousJoyState.Buttons[(int)button];        
    }

    public bool GenButtonReleased(Buttons button)
    {
        return !currentJoyState.Buttons[(int)button] &&
            previousJoyState.Buttons[(int)button];        
    }

    public bool IsGenButtonPressed(Buttons button)
    {
        return currentJoyState.Buttons[(int)button];        
    }

    

    public bool GenHatPressed(SwitchHats hats)
    {
        switch((int)hats)
        {
            case 0: // UP
                return currentJoyState.PointOfViewControllers[0] == 0;
            case 1: // UPRIGHT
                return currentJoyState.PointOfViewControllers[0] == 4500;
            case 2: // RIGHT
                return currentJoyState.PointOfViewControllers[0] == 9000;
            case 3: // DOWNRIGHT
                return currentJoyState.PointOfViewControllers[0] == 13500;
            case 4: // DOWN
                return currentJoyState.PointOfViewControllers[0] == 18000;
            case 5: // DOWNLEFT
                return currentJoyState.PointOfViewControllers[0] == 22500;
            case 6: // LEFT
                return currentJoyState.PointOfViewControllers[0] == 27000;
            case 7: // UPLEFT
                return currentJoyState.PointOfViewControllers[0] == 31500;
        }
        return false;
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