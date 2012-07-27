(defmacro push (item listvar)
  (list 'setq listvar (list 'cons item listvar)))

(defmacro pop (listvar)
  (list
    (list 'lambda '(tos)
      (list 'setq listvar (list 'cdr listvar))
      'tos)
    (list 'car listvar)))

(defun <= (a b)
  (not (> a b)))

(defun >= (a b)
  (not (< a b)))
