class Example {

   static List sortList(input, sep) {
      input.sort { str1, str2 ->
         str1.tokenize(sep).size() <=> str2.tokenize(sep).size()
      }
      return input
   }

   static void main(String[] args) {
      List lst = ["123abc_4", "123abd", "123abc", "123abd_1", "123abd_1_3"]
      List newlst = sortList(lst, '_')
      // newlst.each { -> item
         // println('' + item)
      // }
      println(newlst)
      // Using a simple println statement to print output to the console
      println('Done.');
   }
}