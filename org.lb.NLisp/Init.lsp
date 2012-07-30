; vim: et:lisp:ai

; TODO: equalp, assoc, string manipulation functions, cond macro

(defun list (&rest args) args)
(defun not (x) (if x nil t))
(defun <= (a b) (not (> a b)))
(defun >= (a b) (not (< a b)))
(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun caaar (x) (car (car (car x))))
(defun caadr (x) (car (car (cdr x))))
(defun cadar (x) (car (cdr (car x))))
(defun caddr (x) (car (cdr (cdr x))))
(defun cdaar (x) (cdr (car (car x))))
(defun cdadr (x) (cdr (car (cdr x))))
(defun cddar (x) (cdr (cdr (car x))))
(defun cdddr (x) (cdr (cdr (cdr x))))
(defun abs (x) (if (< x 0) (- 0 x) x))
(defun evenp (x) (= 0 (mod x 2)))
(defun oddp (x) (not (evenp x)))

(defmacro incf (varname)
  (list 'setq varname (list '+ varname 1)))

(defmacro decf (varname)
  (list 'setq varname (list '- varname 1)))

(defmacro push (item listvar)
  (list 'setq listvar (list 'cons item listvar)))

(defmacro pop (listvar)
  (define sym (gensym))
  (list
    (list 'lambda (list sym)
      (list 'setq listvar (list 'cdr listvar))
      sym)
    (list 'car listvar)))

(defun map (f lst)
  (define ret nil)
  (while lst
    (push (f (pop lst)) ret))
  (nreverse ret))

(defun filter (f lst)
  (define ret nil)
  (while lst
    (if (f (car lst))
        (push (car lst) ret)
        nil)
    (setq lst (cdr lst)))
  (nreverse ret))

(defun reduce (f lst)
  (define acc (pop lst))
  (if lst
      (while lst
        (setq acc (f acc (pop lst))))
      (setq acc nil))
  acc)

(defmacro let (variable-list &rest body)
  (push (map car variable-list) body)
  (push 'lambda body)
  (+ (list body) (map cadr variable-list)))

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

(defun range (&rest args)
  (defun values (from to step)
    (define ret nil)
    (while (< from to)
      (push from ret)
      (setq from (+ from step)))
    (reverse ret))
  (define arglen (length args))
  (if (= 1 arglen)
      (values 0 (car args) 1)
      (if (> arglen 2)
          (values (car args) (cadr args) (caddr args))
          (values (car args) (cadr args) 1))))

(defmacro and (&rest args)
  (defun expand (x)
    (if (cdr x)
        (list 'if (car x) (expand (cdr x)) nil)
        (car x)))
  (expand args))

(defmacro or (&rest args)
  (define sym (gensym))
  (defun expand (x)
    (if (cdr x)
        (list 'progn (list 'setq sym (car x)) (list 'if sym sym (expand (cdr x))))
        (car x)))
  (list (list 'lambda '()
    (list 'define sym 'nil)
    (expand args))))

(defmacro dolist (var-list-form &rest body)
  (let ((var         (car var-list-form))
        (list-form   (cadr var-list-form))
        (result-form (caddr var-list-form))
        (sym         (gensym)))
    (push (list 'setq var (list 'pop sym)) body)
    (push sym body)
    (push 'while body)
    (list 'let (list (list sym list-form)
                     (list var 'nil))
      body
      (list 'setq var 'nil)
      (or result-form 'nil))))

