using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Text.RegularExpressions;

public class Graph : MonoBehaviour
{
    [SerializeField]
    InputField expr; //????????? ??????

    [SerializeField]
    LineRenderer linePrefab; //?????? ????? ??? ???????

    [SerializeField]
    RawImage img; //??????? ????????? ???????

    [SerializeField]
    GameObject Lines; //?????? ?????? ??? ??????????? ?????????? ????? (???? ? ??????? ?????????? ???????)

    [SerializeField]
    double xindStart = 1;
    [SerializeField]
    double xindEnd = 1;
    [SerializeField]
    double xStep = 1;

    //C?????? ??? ??????????? ????????? ?????? ?????
    static Dictionary<string, string> replace = new Dictionary<string, string>()
    {
        ["asinh"] = "Asinh",
        ["asin"] = "Asin",
        ["sinh"] = "Sinh",
        ["sin"] = "Sin",

        ["acosh"] = "Acosh",
        ["acos"] = "Acos",
        ["cosh"] = "Cosh",
        ["cos"] = "Cos",

        ["atanh"] = "Atanh",
        ["atan"] = "Atan",
        ["tanh"] = "Tanh",
        ["tan"] = "Tan",

        ["ASin"] = "Asin",
        ["ACos"] = "Acos",
        ["ATan"] = "Atan",

        ["sqrt"] = "Sqrt",
        ["sqrt"] = "Sqrt",
        ["log"] = "Log",
        ["ln"] = "Log",
        ["Ln"] = "Log",
        ["abs"] = "Abs",
        ["pow"] = "Pow",
        ["pi"] = $"({Math.PI})",
        ["Pi"] = $"({Math.PI})",
        ["PI"] = $"({Math.PI})",
        [","] = ".",
        ["X"] = "x",
        [" "] = ""
    }; 

    //??????? ????????? ??????? ?????? ? ?????????? ???????? ? ????????? ??????????
    public void button_click()
    {
        foreach (Transform child in Lines.transform)
        {
            Destroy(child.gameObject);
        }

        linePrefab.positionCount = 0;
        var expression = expr.text;
        expression = Replace(expression);
        expression = Multiplic(expression);
        expression = _Pow(expression);
        expression = Func_name(expression);


        ParseAndDraw(expression, xindStart, xindEnd, xStep, linePrefab, img, Lines);
    }

    //??????? ? ???????? ? ??????????
    static async void ParseAndDraw(string expression, double xindStart, double xindEnd, double xStep, LineRenderer linePrefab, RawImage img, GameObject Lines)
    {
        var options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.WithImports("System.Math"); //?????????? ???? Math
        var func = await CSharpScript.EvaluateAsync<Func<double, double>>($"x => {expression.ToString()}", options); //???????? ????????

        LineRenderer line = Instantiate(linePrefab, Lines.transform);
        line.positionCount = 0;


        //?????????? ?????
        int i = 0;
        double max = -1000;
        double min = 1000;
        List<Vector2> arr = new List<Vector2>();
        for (double x = xindStart; x <= xindEnd + xStep; x += xStep)
        {
            double y = func(x);
            arr.Add(new Vector2((float)(x), (float)(y)));
            if (y > max) max = y;
            if (y < min) min = y;
        }

        if (max == min)
        {
            min = -max;
        }
        else
        {
            if (max > 100) max = 20;
            if (min < -100) min = -20;
        }

        //??????????? ??? ??????????? ???????
        float kY = 1f, kX = 1f;
        double realSizeX = Math.Abs(xindEnd - xindStart + 1);
        double graphSizeX = img.GetComponent<RectTransform>().sizeDelta.x * 0.78 * img.transform.parent.transform.parent.GetComponent<RectTransform>().transform.localScale.x;
        kX = (float)(realSizeX / graphSizeX);

        double realSizeY = Math.Abs(max - min + 1);
        double graphSizeY = img.GetComponent<RectTransform>().sizeDelta.y * 0.4 * img.transform.parent.transform.parent.GetComponent<RectTransform>().transform.localScale.y;
        kY = (float)(realSizeY / graphSizeY);

        Debug.Log("????? (x,y): " + kX + " , " + kY);

        //??????????
        int cnt;
        for (i = 0, cnt = 0; cnt < arr.Count; i++, cnt++)
        {
            if (cnt > 0 && (arr[cnt - 1].y - arr[cnt].y) > 100)
            {
                line = Instantiate(linePrefab, Lines.transform);
                line.positionCount = 0;
                i = 0;
            }
            line.positionCount++;
            line.SetPosition(i, new Vector2(arr[cnt].x * kX, arr[cnt].y / kY));
        }
    }

    static string Replace(string expr) //?????? ?? ???????
    {
        foreach (var item in replace)
        {
            expr = expr.Replace(item.Key, item.Value);
        }
        return expr;
    }

    static string Func_name(string expr) //???????? ??? ???????, ???? ???? ??????? ("y = ", "z = " ? ?.?.)
    {
        int i = 0;
        while (i < expr.Length)
        {
            if (expr[i] == '=')
            {
                string[] buffer = new string[2];
                buffer[0] = expr.Substring(0, i);
                buffer[1] = expr.Substring(i + 1, expr.Length - 1 - i);

                if (buffer[0] == "")
                {
                    break;
                }

                expr = buffer[1];
                break;
            }
            i++;
        }

        return expr;
    } 

    static string _Pow(string expr) //????????? ???????, ????????? ????? "^" ("2^3", (x+2)^(x-1), (x+2)^2 ? ?.?.)
    {
        var i = 0;
        while (i < expr.Length)
        {
            if (expr[i] == '^')
            {
                if (expr[i - 1] == ')')
                {
                    var leftbracketscount = 1;
                    var left_indEnd = i - 1;
                    var left_indStart = 0;
                    var _i = i - 2;

                    while (_i >= 0 && leftbracketscount != 0)
                    {
                        if (expr[_i] == '(')
                            leftbracketscount--;
                        if (expr[_i] == ')')
                            leftbracketscount++;
                        _i--;
                    }

                    if (leftbracketscount != 0)
                    {
                        //Debug.Log("?????? ? ????????? ???????");
                    }
                    else
                    {
                        left_indStart = _i + 1;
                        char[] temp = new char[left_indEnd - left_indStart + 1];
                        //Debug.Log("?????? ?????? ? ????? ?????????????: " + left_indStart + "," + left_indEnd);
                        for (int k = left_indStart, c = 0; k <= left_indEnd; k++, c++)
                        {
                            temp[c] = expr[k];
                        }

                        string Pow_leftside = new string(temp);
                        //Debug.Log("?????, ??????? ?????????? ? ???????:" + Pow_leftside);

                        string[] buffer = new string[2];
                        buffer[0] = expr.Substring(0, left_indStart);
                        buffer[1] = expr.Substring(left_indEnd + 1, expr.Length - 1 - left_indEnd);

                        //Debug.Log("????? ?? ?????????????: " + buffer[0]);
                        //Debug.Log("????? ????? ?????????????: " + buffer[1]);
                        expr = buffer[0] + "(Pow(" + Pow_leftside + "," + buffer[1];

                        //Debug.Log("??????????? ??????: " + expr);

                        for (i = 0; i < expr.Length; i++)
                        {
                            if (expr[i] == '^')
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var left_indEnd = i - 1;
                    var left_indStart = i - 1;

                    string signs = "+-*/";

                    while (left_indStart >= 0)
                    {
                        int j;
                        for (j = 0; j < signs.Length; j++)
                        {
                            if (expr[left_indStart] == signs[j])
                            {
                                j = -1;
                                break;
                            }
                        }

                        if (j == -1)
                        {
                            left_indStart++;
                            break;
                        }
                        left_indStart--;
                    }

                    if (left_indStart == -1) left_indStart++;

                    char[] temp = new char[left_indEnd - left_indStart + 1];
                    //Debug.Log("?????? ?????? ? ????? ?????????????: " + left_indStart + "," + left_indEnd);
                    for (int k = left_indStart, c = 0; k <= left_indEnd; k++, c++)
                    {
                        temp[c] = expr[k];
                    }

                    string Pow_leftside = new string(temp);
                    //Debug.Log("?????, ??????? ?????????? ? ???????: " + Pow_leftside);

                    string[] buffer = new string[2];
                    buffer[0] = expr.Substring(0, left_indStart);
                    buffer[1] = expr.Substring(left_indEnd + 1, expr.Length - 1 - left_indEnd);

                    //Debug.Log("????? ?? ?????????????: " + buffer[0]);
                    //Debug.Log("????? ????? ?????????????: " + buffer[1]);
                    expr = buffer[0] + "(Pow(" + Pow_leftside + "," + buffer[1];

                    //Debug.Log("??????????? ??????: " + expr);

                    for (i = 0; i < expr.Length; i++)
                    {
                        if (expr[i] == '^')
                        {
                            break;
                        }
                    }
                }

                if (expr[i + 1] == '(')
                {
                    var rightbracketscount = 1;
                    var right_indEnd = 0;
                    var right_indStart = i + 1;
                    var _i = i + 2;

                    while (_i <= expr.Length && rightbracketscount != 0)
                    {
                        if (expr[_i] == '(')
                            rightbracketscount++;
                        if (expr[_i] == ')')
                            rightbracketscount--;
                        _i++;
                    }

                    if (rightbracketscount != 0)
                    {
                        //Debug.Log("?????? ? ????????? ???????");
                        return expr;
                    }
                    else
                    {
                        right_indEnd = _i - 1;
                        char[] temp = new char[right_indEnd - right_indStart + 1];
                        //Debug.Log("?????? ?????? ? ????? ?????????????: " + right_indStart + "," + right_indEnd);
                        for (int k = right_indStart, c = 0; k <= right_indEnd; k++, c++)
                        {
                            temp[c] = expr[k];
                        }

                        string Pow_rightside = new string(temp);
                        //Debug.Log("???????:" + Pow_rightside);

                        string[] buffer = new string[2];
                        buffer[0] = expr.Substring(0, right_indStart);
                        buffer[1] = expr.Substring(right_indEnd + 1, expr.Length - 1 - right_indEnd);

                        buffer[0] = buffer[0].Remove(buffer[0].Length - 1);
                        //Debug.Log("????? ?? ???????: " + buffer[0]);
                        //Debug.Log("????? ????? ???????: " + buffer[1]);
                        expr = buffer[0] + Pow_rightside + "))" + buffer[1];

                        //Debug.Log("??????????? ??????: " + expr);
                    }
                }
                else
                {
                    var right_indEnd = i + 1;
                    var right_indStart = i + 1;

                    string signs = "+-*/";

                    while (right_indEnd < expr.Length)
                    {
                        int j;
                        for (j = 0; j < signs.Length; j++)
                        {
                            if (expr[right_indEnd] == signs[j])
                            {
                                j = -1;
                                break;
                            }
                        }

                        if (j == -1)
                        {
                            right_indEnd--;
                            break;
                        }
                        right_indEnd++;
                    }

                    if (right_indEnd == expr.Length) right_indEnd--;

                    char[] temp = new char[right_indEnd - right_indStart + 1];
                    //Debug.Log("?????? ?????? ? ????? ?????????????: " + right_indStart + "," + right_indEnd);
                    for (int k = right_indStart, c = 0; k <= right_indEnd; k++, c++)
                    {
                        temp[c] = expr[k];
                    }

                    string Pow_rightside = new string(temp);
                    //Debug.Log("???????:" + Pow_rightside);

                    string[] buffer = new string[2];
                    buffer[0] = expr.Substring(0, right_indStart);
                    buffer[1] = expr.Substring(right_indEnd + 1, expr.Length - 1 - right_indEnd);

                    buffer[0] = buffer[0].Remove(buffer[0].Length - 1);
                    //Debug.Log("????? ?? ???????: " + buffer[0]);
                    //Debug.Log("????? ????? ???????: " + buffer[1]);
                    expr = buffer[0] + Pow_rightside + "))" + buffer[1];

                    //Debug.Log("??????????? ??????: " + expr);
                }


                i++;
            }
            else
            {
                i++;
            }
        }
        return expr;
    }

    static string Multiplic(string expr) //?????????? ??????????? ?????? ( x(2+1) -> x*(2+1) )
    {
        Regex beforePattern = new Regex(@"[0-9x]{1}[a-zA-Z(]");
        Regex afterPattern = new Regex(@"\)[0-9a-zA-Z(]");

        while (afterPattern.Matches(expr.ToString()).Count != 0)
        {
            Match match2 = afterPattern.Match(expr.ToString());
            expr = expr.Replace(match2.ToString(), $"{match2.ToString()[0]}*{match2.ToString()[1]}");
        }
        while (beforePattern.Matches(expr.ToString()).Count != 0)
        {
            Match match3 = beforePattern.Match(expr.ToString());
            expr = expr.Replace(match3.ToString(), $"{match3.ToString()[0]}*{match3.ToString()[1]}");
        }

        return expr;
    }

}
