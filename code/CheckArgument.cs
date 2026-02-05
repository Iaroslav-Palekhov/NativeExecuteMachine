using static Interpreter;

struct CheckArgument{
    
    public static bool Check(byte countArguments){
        
        if (parts.Count() > countArguments){
            return true;
        }

        return false;
    }
}