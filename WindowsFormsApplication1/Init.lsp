; TODO:
; let over lambda (=> rewrite pop macro)
; equalp, and, or, assoc, ...

(defun not (x) (if x nil t))
(defun <= (a b) (not (> a b)))
(defun >= (a b) (not (< a b)))
(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))

(defun every (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc (f item) nil))
        (cons t lst))
      t))

(defun some (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc acc (f item)))
        (cons nil lst))
      nil))

(defmacro push (item listvar)
  (list 'setq listvar (list 'cons item listvar)))

(defmacro pop (listvar)
  (list
    (list 'lambda '(tos)
      (list 'setq listvar (list 'cdr listvar))
      'tos)
    (list 'car listvar)))
