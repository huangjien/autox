package autox.actions;

import autox.log.Log;
import autox.utils.XML;
import org.apache.axis.utils.StringUtils;
import org.jdom.Content;
import org.jdom.Element;

/**
 * Created with AutoX project.
 * User: jien.huang
 * Date: 12/14/12
 */
public class Result {
    public static final String RESULT = "Result";
    public static final String SUCCESS = "Success";
    public static final String FAILED = "Failed";
    public static final String REASON = "Reason";
    public static final String ORIGINAL = "Original";
    Element result = new Element(RESULT);

    public Result(Element element) {

        //setOriginal(element);
        this.Success();
    }
    public void addSubResult(Result subResult){
        if(subResult.result!=null){
            result.addContent((Content)subResult.result.clone());
        }
    }
    public void setOriginal(Element element) {
        if (element != null) {
            Element original = new Element(ORIGINAL);
            original.addContent((Content) element.clone());
            result.addContent(original);
        }
    }

    public static String failed(String s) {
        Result failedResult = failedResult(s);
        return failedResult.toString();
    }

    private static Result failedResult(String s) {
        Result failedResult = new Result(null);
        failedResult.Error(s);
        return failedResult;
    }

    public boolean isSuccess() {
        String resultString = result.getAttributeValue(RESULT);
        if(StringUtils.isEmpty(resultString))
            return true;
        return resultString.equalsIgnoreCase(SUCCESS);
    }

    public static Result fromString(String resultString) {
        try {
            Element stringResult = new XML(resultString).getRoot();
            if (!stringResult.getName().equalsIgnoreCase(RESULT)) {
                return failedResult("Return is not a result");
            } else {
                Result newResult = new Result(null);
                newResult.result = stringResult;
                return newResult;
            }
        } catch (Exception e) {
            Log.fatal(e.getMessage(), e);
            return failedResult(e.getMessage());
        }

    }

    public void Error(String s) {
        result.setAttribute(RESULT, FAILED);
        Element reason = new Element(REASON);
        reason.setAttribute(REASON, s);
        result.addContent(reason);
    }

    public Element toElement() {
        return result;
    }

    public String toString(){
        return XML.toString(result);
    }

    public void Success() {
        if (isSuccess())
            result.setAttribute(RESULT, SUCCESS);
    }
}
