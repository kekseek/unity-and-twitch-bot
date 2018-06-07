using System;


public class ChatMoveCommand {
    public enum MoveType {
        left, right, forward, back
    }

    public readonly DateTime time;
    public readonly MoveType type;

    public ChatMoveCommand(MoveType type) {
        time = DateTime.Now;
        this.type = type;
    }
}

